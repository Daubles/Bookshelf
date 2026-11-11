namespace Bookshelf.Data.Books;

/// <summary>
///     Join between a work and its authors, keeping the author order from Open Library.
/// </summary>
public class WorkAuthor
{
    public int WorkId { get; init; }
    public Work Work { get; init; } = null!;

    public int AuthorId { get; init; }
    public Author Author { get; init; } = null!;

    public int Position { get; set; }
}
