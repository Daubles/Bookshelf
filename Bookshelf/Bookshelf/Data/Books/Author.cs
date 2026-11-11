namespace Bookshelf.Data.Books;

public class Author
{
    public int Id { get; init; }
    public required string OpenLibraryKey { get; init; }

    public required string Name { get; set; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? Bio { get; set; }

    // Open Library dates are free text ("1920", "c. 1564", "8 June 1949"), so keep them as strings.
    public string? BirthDate { get; set; }
    public string? DeathDate { get; set; }

    /// <summary>Open Library author photo ID.</summary>
    public int? PhotoId { get; set; }

    public DateTimeOffset LastFetchedAt { get; set; }
    
    public List<WorkAuthor> Works { get; set; } = [];
}
