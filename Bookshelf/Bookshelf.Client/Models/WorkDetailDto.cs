namespace Bookshelf.Client.Models;

/// <summary>
/// Everything the book page shows: the work itself plus its authors.
/// </summary>
public class WorkDetailDto
{
    public required string OpenLibraryKey { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public string? Description { get; init; }
    public int? FirstPublishYear { get; init; }
    public int? CoverId { get; init; }
    public string? Publisher { get; init; }
    public int? NumberOfPages { get; init; }
    public List<AuthorSummaryDto> Authors { get; init; } = [];
}

public class AuthorSummaryDto
{
    public required string OpenLibraryKey { get; set; }
    public required string Name { get; init; }
}
