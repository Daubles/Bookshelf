namespace Bookshelf.Client.Models;

public class BookSummaryDto
{
    public required string OpenLibraryKey { get; set; }
    public required string Title { get; set; }
    public required string[] AuthorName { get; set; }
    public int FirstPublishYear { get; set; }
}