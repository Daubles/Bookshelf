namespace Bookshelf.Data.Books;

/// <summary>
/// The overall book, independent of any particular edition.
/// </summary>
public class Work
{
    public int Id { get; init; }
    public required string OpenLibraryKey { get; init; }

    public required string Title { get; init; }
    public string? Subtitle { get; set; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? Description { get; set; }
    public int? FirstPublishYear { get; set; }

    // ID to retrieve cover image from Open Library
    public int? CoverId { get; set; }

    // Open Library only has these per edition, so they're copied from one chosen edition.
    public string? Publisher { get; set; }
    public int? NumberOfPages { get; set; }

    /// <summary>Open Library key of the edition the publisher and page count came from, e.g. "OL7353617M".</summary>
    public string? SourceEditionKey { get; set; }

    public DateTimeOffset LastFetchedAt { get; set; }

    public List<WorkAuthor> Authors { get; set; } = [];
}
