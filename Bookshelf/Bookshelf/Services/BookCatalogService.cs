using System.Text.RegularExpressions;
using Bookshelf.Client.Models;
using Bookshelf.Data.Books;
using Bookshelf.Models;
using static Bookshelf.Services.OpenLibraryClient;

namespace Bookshelf.Services;

/// <summary>
/// Decides where book data comes from. Search always goes to Open Library and stores nothing.
/// A work and its authors are stored the first time the book page is opened (with publisher and
/// page count taken from one of its editions), then served from the database until they go stale.
/// </summary>
public partial class BookCatalogService(OpenLibraryClient openLibrary, BookRepository repository, ILogger<BookCatalogService> logger)
{
    private static readonly TimeSpan RefreshAfter = TimeSpan.FromDays(30);
    private const int EditionsToFetch = 50;
    private const int MaxSaveAttempts = 2;

    // Column limits from AppDbContext; values are trimmed to fit rather than failing the save.
    private const int WorkTitleMax = 500;
    private const int SubtitleMax = 500;
    private const int PublisherMax = 300;
    private const int AuthorNameMax = 300;
    private const int DateMax = 50;

    public async Task<List<BookSummaryDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var result = await openLibrary.SearchAsync(query, limit, ct);

        return result?.Docs
            .Select(doc => new BookSummaryDto
            {
                OpenLibraryKey = StripPrefix(doc.Key),
                Title = doc.Title,
                AuthorName = doc.AuthorName ?? [],
                FirstPublishYear = doc.FirstPublishYear,
            })
            .ToList() ?? [];
    }

    /// <summary>
    /// Returns the work with its authors, or null if Open Library has no such work.
    /// Throws <see cref="HttpRequestException"/> if Open Library can't be reached and nothing is stored locally.
    /// </summary>
    public async Task<WorkDetailDto?> GetWorkAsync(string workKey, CancellationToken ct = default)
    {
        if (!WorkKeyRegex().IsMatch(workKey))
            return null;

        // Grab work detail from our db if we have it stored and return it if it doesn't need updating
        var work = await repository.GetWorkWithDetailsAsync(workKey, ct);
        if (IsFresh(work))
            return work!.ToDetailDto();

        OpenLibraryWork? olWork;
        try
        {
            olWork = await openLibrary.GetWorkAsync(workKey, ct);
        }
        catch (HttpRequestException ex) when (work is not null)
        {
            // Open Library is down: a stale copy is better than an error.
            logger.LogWarning(ex, "Couldn't refresh work {WorkKey} from Open Library; serving stored copy", workKey);
            return work.ToDetailDto();
        }

        if (olWork is null)
        {
            if (work is null)
                return null;

            // Gone from Open Library (deleted, or no longer a usable work). Keep serving our copy,
            // but mark it checked so every page view doesn't ask Open Library again.
            logger.LogInformation("Work {WorkKey} is no longer on Open Library; serving stored copy", workKey);
            work.LastFetchedAt = DateTimeOffset.UtcNow;
            await repository.TrySaveChangesAsync(ct);
            return work.ToDetailDto();
        }

        // The key can come from an old bookmarked URL or a stale search result for a work that
        // Open Library has since merged into another. Works are stored under their current key,
        // so look that one up too.
        var finalKey = StripPrefix(olWork.Key);
        if (finalKey != workKey)
        {
            work = await repository.GetWorkWithDetailsAsync(finalKey, ct);
            if (IsFresh(work))
                return work!.ToDetailDto();
        }

        // Editions only fill fields that are still empty, so skip the (large) request when none are.
        var editionsTask = NeedsEditionData(work)
            ? openLibrary.GetEditionsAsync(finalKey, EditionsToFetch, ct)
            : Task.FromResult(new List<OpenLibraryEdition>());
        var authorsTask = GetAuthorsAsync(olWork, ct);
        var olEditions = await editionsTask;
        var (authorKeys, olAuthors) = await authorsTask;

        for (var attempt = 1; ; attempt++)
        {
            work = await ApplyWorkAsync(work, finalKey, olWork, authorKeys, olAuthors, olEditions, ct);
            if (await repository.TrySaveChangesAsync(ct))
                return work.ToDetailDto();

            // Another request stored this work, or one of its authors, at the same moment. Its rows
            // are in the database now, so reload: use its copy of the work if it saved one,
            // otherwise apply again on top of the authors it stored.
            work = await repository.GetWorkWithDetailsAsync(finalKey, ct);
            if (IsFresh(work))
                return work!.ToDetailDto();
            if (attempt == MaxSaveAttempts)
                throw new InvalidOperationException($"Couldn't save work {finalKey}.");
        }
    }

    private static bool IsFresh(Work? work) =>
        work is not null && DateTimeOffset.UtcNow - work.LastFetchedAt < RefreshAfter;

    private static bool NeedsEditionData(Work? work) =>
        work is null || work.Publisher is null || work.NumberOfPages is null
        || work.CoverId is null || work.FirstPublishYear is null;

    /// <summary>
    /// Fetches the work's authors. Returns their keys in order (each author's current key, after
    /// following merges) and the Open Library data for each one that could be fetched.
    /// A key whose fetch failed is kept as-is, so a stored author stays linked.
    /// </summary>
    private async Task<(List<string> Keys, Dictionary<string, OpenLibraryAuthor> Authors)> GetAuthorsAsync(
        OpenLibraryWork olWork, CancellationToken ct)
    {
        var requestedKeys = (olWork.Authors ?? [])
            .Select(a => a.Author?.Key)
            .OfType<string>()
            .Select(StripPrefix)
            .Distinct()
            .ToList();

        var fetched = await Task.WhenAll(requestedKeys.Select(key => TryGetAuthorAsync(key, ct)));

        var keys = new List<string>();
        var authors = new Dictionary<string, OpenLibraryAuthor>();
        for (var i = 0; i < requestedKeys.Count; i++)
        {
            var key = fetched[i] is { } olAuthor ? StripPrefix(olAuthor.Key) : requestedKeys[i];
            if (fetched[i] is not null)
                authors.TryAdd(key, fetched[i]!);
            // Two listed authors can have been merged into the same one.
            if (!keys.Contains(key))
                keys.Add(key);
        }

        return (keys, authors);
    }

    private async Task<OpenLibraryAuthor?> TryGetAuthorAsync(string authorKey, CancellationToken ct)
    {
        try
        {
            return await openLibrary.GetAuthorAsync(authorKey, ct);
        }
        catch (HttpRequestException ex)
        {
            // One author failing shouldn't stop the whole book from loading.
            logger.LogWarning(ex, "Couldn't fetch author {AuthorKey}", authorKey);
            return null;
        }
    }

    /// <summary>
    ///     Creates or updates the tracked work and its authors from Open Library data. Doesn't save.
    /// </summary>
    private async Task<Work> ApplyWorkAsync(
        Work? work,
        string workKey,
        OpenLibraryWork olWork,
        List<string> authorKeys,
        Dictionary<string, OpenLibraryAuthor> olAuthors,
        List<OpenLibraryEdition> olEditions,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Work out every value from the Open Library data first, then either create or update.
        // OpenLibraryClient.GetWorkAsync only returns works that have a title.
        var title = Truncate(olWork.Title!, WorkTitleMax)!;
        var subtitle = Truncate(olWork.Subtitle, SubtitleMax);

        // Publisher and page count only exist on editions, so take them from one chosen edition.
        var sourceEdition = ChooseSourceEdition(olEditions);
        var publisher = Truncate(sourceEdition?.Publishers?.FirstOrDefault(), PublisherMax);
        var numberOfPages = sourceEdition?.NumberOfPages is > 0 ? sourceEdition.NumberOfPages : null;
        var sourceEditionKey = sourceEdition is null ? null : StripPrefix(sourceEdition.Key);

        // Cover: the work's own first cover, then the chosen edition's, then any edition's.
        var coverId = FirstCover(olWork.Covers)
                      ?? FirstCover(sourceEdition?.Covers)
                      ?? olEditions.Select(e => FirstCover(e.Covers)).FirstOrDefault(c => c is not null);

        // Works rarely carry a year; fall back to the earliest edition year Open Library lists.
        var firstPublishYear = ParseYear(olWork.FirstPublishDate)
                               ?? olEditions.Select(e => ParseYear(e.PublishDate)).Where(y => y is not null).Min();

        if (work is null)
        {
            work = new Work
            {
                OpenLibraryKey = workKey,
                Title = title,
                Subtitle = subtitle,
                Description = olWork.Description,
                Publisher = publisher,
                NumberOfPages = numberOfPages,
                SourceEditionKey = sourceEditionKey,
                CoverId = coverId,
                FirstPublishYear = firstPublishYear,
                LastFetchedAt = now,
            };
            repository.AddWork(work);
        }
        else
        {
            // On refresh, fields are only filled while empty, so a refresh never overwrites values
            // edited in our database. Open Library rarely changes these fields, and blindly copying
            // them would also pick up vandalism before it's reverted.
            work.Subtitle ??= subtitle;
            work.Description ??= olWork.Description;

            // Publisher, page count and their source edition belong together, so only take them
            // as a set: never record a different source edition for values we already have.
            if (work.Publisher is null || work.NumberOfPages is null)
            {
                work.Publisher ??= publisher;
                work.NumberOfPages ??= numberOfPages;
                work.SourceEditionKey ??= sourceEditionKey;
            }

            work.CoverId ??= coverId;
            work.FirstPublishYear ??= firstPublishYear;
            work.LastFetchedAt = now;
        }

        // Authors are always refreshed: author merges are the changes that actually matter to us.
        await ApplyAuthorsAsync(work, authorKeys, olAuthors, now, ct);

        return work;
    }

    private async Task ApplyAuthorsAsync(
        Work work, List<string> authorKeys, Dictionary<string, OpenLibraryAuthor> olAuthors,
        DateTimeOffset now, CancellationToken ct)
    {
        var existing = await repository.GetAuthorsByKeyAsync(authorKeys, ct);

        var ordered = new List<Author>();
        foreach (var key in authorKeys)
        {
            olAuthors.TryGetValue(key, out var olAuthor);
            existing.TryGetValue(key, out var author);

            if (author is null)
            {
                // Skip authors we have no data for (deleted on Open Library, or the fetch failed).
                if (olAuthor is null) continue;
                author = new Author
                {
                    OpenLibraryKey = key,
                    // OpenLibraryClient.GetAuthorAsync only returns authors that have a name.
                    Name = Truncate(olAuthor.Name!, AuthorNameMax)!,
                    Bio = olAuthor.Bio,
                    BirthDate = Truncate(olAuthor.BirthDate, DateMax),
                    DeathDate = Truncate(olAuthor.DeathDate, DateMax),
                    PhotoId = FirstCover(olAuthor.Photos),
                    LastFetchedAt = now,
                };
                repository.AddAuthor(author);
            }
            else if (olAuthor is not null)
            {
                // Authors are always overwritten on refresh (unlike works).
                author.Name = Truncate(olAuthor.Name!, AuthorNameMax)!;
                author.Bio = olAuthor.Bio;
                author.BirthDate = Truncate(olAuthor.BirthDate, DateMax);
                author.DeathDate = Truncate(olAuthor.DeathDate, DateMax);
                author.PhotoId = FirstCover(olAuthor.Photos);
                author.LastFetchedAt = now;
            }

            ordered.Add(author);
        }

        // Update links in place rather than clearing and re-adding, so EF doesn't track
        // a deleted and an added WorkAuthor with the same key.
        work.Authors.RemoveAll(wa => !ordered.Contains(wa.Author));
        for (var i = 0; i < ordered.Count; i++)
        {
            var link = work.Authors.FirstOrDefault(wa => wa.Author == ordered[i]);
            if (link is null)
                work.Authors.Add(new WorkAuthor { Work = work, Author = ordered[i], Position = i });
            else
                link.Position = i;
        }
    }

    /// <summary>
    ///     Picks the edition to take the publisher and page count from.
    /// </summary>
    private static OpenLibraryEdition? ChooseSourceEdition(List<OpenLibraryEdition> editions) =>
        editions
            .OrderByDescending(e => e.Languages?.Any(l => StripPrefix(l.Key) == "eng") == true)
            .ThenByDescending(e => e is { Publishers.Length: > 0, NumberOfPages: > 0 })
            .ThenBy(e => ParseYear(e.PublishDate) ?? int.MaxValue)
            .FirstOrDefault();

    // Open Library uses -1 for removed covers.
    private static int? FirstCover(int[]? covers) => covers?.FirstOrDefault(id => id > 0) is > 0 and var first ? first : null;

    private static int? ParseYear(string? date) =>
        date is not null && YearRegex().Match(date) is { Success: true } m ? int.Parse(m.Value) : null;

    private static string? Truncate(string? value, int max)
    {
        if (value is null || value.Length <= max)
            return value;

        // Don't cut between the two halves of a surrogate pair (emoji etc.): a lone half isn't
        // valid text and the database would reject it.
        var end = char.IsHighSurrogate(value[max - 1]) ? max - 1 : max;
        return value[..end];
    }

    [GeneratedRegex(@"^OL\d+W$")]
    private static partial Regex WorkKeyRegex();

    [GeneratedRegex(@"\b\d{4}\b")]
    private static partial Regex YearRegex();
}
