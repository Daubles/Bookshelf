namespace Bookshelf.Client.Models;

public enum CoverSize
{
    Small,
    Medium,
    Large,
}

/// <summary>
///     Builds Open Library cover image URLs from stored cover IDs.
/// </summary>
public static class OpenLibraryCovers
{
    private const string BaseUrl = "https://covers.openlibrary.org";
    
    public static string? BookCoverUrl(int? coverId, CoverSize size = CoverSize.Medium) =>
        coverId is > 0 ? $"{BaseUrl}/b/id/{coverId}-{SizeCode(size)}.jpg" : null;

    private static string SizeCode(CoverSize size) => size switch
    {
        CoverSize.Small => "S",
        CoverSize.Large => "L",
        _ => "M",
    };
}
