using System.Net;
using System.Text.Json;
using Bookshelf.Models;

namespace Bookshelf.Services;

/// <summary>
/// Typed HTTP client for the Open Library JSON API.
/// Every failure to get a usable response (network error, error status, timeout, malformed JSON)
/// is thrown as <see cref="HttpRequestException"/>, so callers only need to handle that one type.
/// </summary>
public class OpenLibraryClient(HttpClient http, ILogger<OpenLibraryClient> logger)
{
    private const int MaxRedirects = 3;
    private const string RedirectType = "/type/redirect";

    public Task<OpenLibraryBookSearchResult?> SearchAsync(string query, int limit, CancellationToken ct = default) =>
        GetOrNullAsync<OpenLibraryBookSearchResult>(
            $"search.json?q={Uri.EscapeDataString(query)}&limit={limit}" +
            "&fields=key,title,author_name,first_publish_year", ct);

    /// <summary>
    /// Fetches a work, following redirects for works Open Library has merged into another.
    /// The returned work's key may therefore differ from <paramref name="workKey"/>.
    /// Returns null for missing works, and for deleted or untitled records, so a returned work
    /// always has a <see cref="OpenLibraryWork.Title"/>.
    /// </summary>
    public async Task<OpenLibraryWork?> GetWorkAsync(string workKey, CancellationToken ct = default)
    {
        var work = await GetRecordAsync<OpenLibraryWork>("works", workKey, "/type/work", ct);
        return string.IsNullOrWhiteSpace(work?.Title) ? null : work;
    }

    public async Task<List<OpenLibraryEdition>> GetEditionsAsync(string workKey, int limit, CancellationToken ct = default)
    {
        var page = await GetOrNullAsync<OpenLibraryEditionsPage>($"works/{workKey}/editions.json?limit={limit}", ct);
        return page?.Entries.ToList() ?? [];
    }

    /// <summary>
    /// Fetches an author, following redirects for authors Open Library has merged into another.
    /// The returned author's key may therefore differ from <paramref name="authorKey"/>.
    /// Returns null for missing, deleted or unnamed authors, so a returned author always has a
    /// <see cref="OpenLibraryAuthor.Name"/>.
    /// </summary>
    public async Task<OpenLibraryAuthor?> GetAuthorAsync(string authorKey, CancellationToken ct = default)
    {
        var author = await GetRecordAsync<OpenLibraryAuthor>("authors", authorKey, "/type/author", ct);
        return string.IsNullOrWhiteSpace(author?.Name) ? null : author;
    }

    public static string StripPrefix(string key) => key[(key.LastIndexOf('/') + 1)..];

    /// <summary>
    /// Fetches a record, following merge redirects. Returns null if it's missing or isn't of
    /// <paramref name="expectedType"/> (usually because it was deleted: Open Library serves
    /// deleted records with a 200 and <c>/type/delete</c>).
    /// </summary>
    private async Task<T?> GetRecordAsync<T>(string collection, string key, string expectedType, CancellationToken ct)
        where T : class, IOpenLibraryRecord
    {
        var originalKey = key;
        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            var record = await GetOrNullAsync<T>($"{collection}/{key}.json", ct);
            if (record is null)
                return null;

            var type = record.Type?.Key;
            if (type == expectedType)
                return record;

            if (type != RedirectType)
            {
                logger.LogInformation("Ignoring {Collection}/{Key}: type {Type}", collection, key, type);
                return null;
            }

            if (record.Location is null)
                return null;
            key = StripPrefix(record.Location);
        }

        logger.LogWarning("Too many redirects resolving {Collection}/{Key}", collection, originalKey);
        return null;
    }

    private async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation; the caller didn't cancel.
            throw new HttpRequestException($"Open Library request timed out: {url}", ex);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException($"Open Library returned malformed JSON: {url}", ex);
        }
    }
}
