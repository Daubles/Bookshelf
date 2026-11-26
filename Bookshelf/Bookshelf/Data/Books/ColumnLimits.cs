namespace Bookshelf.Data.Books;

/// <summary>
/// Maximum lengths of the book tables' string columns. Used by <see cref="AppDbContext"/> to size the
/// columns and by the catalog service to trim Open Library data to fit. Changing one needs a migration.
/// </summary>
public static class ColumnLimits
{
    /// <summary>Open Library keys, e.g. "OL893414W" or "OL7353617M".</summary>
    public const int OpenLibraryKey = 20;

    public const int WorkTitle = 500;
    public const int Subtitle = 500;
    public const int Publisher = 300;

    public const int AuthorName = 300;
    public const int Date = 50;
}
