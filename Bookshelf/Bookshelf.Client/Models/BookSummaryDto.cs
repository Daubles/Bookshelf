namespace Bookshelf.Client.Models;

public class BookSummaryDto
{
    /// <summary>
    ///     Open Library work key without the "/works/" prefix, e.g. "OL45804W".
    /// </summary>
    public required string OpenLibraryKey { get; set; }
    public required string Title { get; set; }
    public string[] AuthorName { get; set; } = [];
    public int? FirstPublishYear { get; set; }
}