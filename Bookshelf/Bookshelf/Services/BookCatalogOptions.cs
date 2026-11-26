using System.ComponentModel.DataAnnotations;

namespace Bookshelf.Services;

/// <summary>
/// Tuning for <see cref="BookCatalogService"/>, bound from the "BookCatalog" config section.
/// Every setting is optional; the defaults here apply when it's left out.
/// </summary>
public class BookCatalogOptions
{
    public const string SectionName = "BookCatalog";

    /// <summary>How long a stored work is served before it's refreshed from Open Library.</summary>
    [Range(typeof(TimeSpan), "00:00:00", "365.00:00:00")]
    public TimeSpan RefreshAfter { get; set; } = TimeSpan.FromDays(30);

    /// <summary>How many editions to look through for publisher, page count, cover and year.</summary>
    [Range(1, 1000)]
    public int EditionsToFetch { get; set; } = 50;

    /// <summary>Save attempts before giving up when another request stores the same work at the same moment.</summary>
    [Range(1, 10)]
    public int MaxSaveAttempts { get; set; } = 2;

    /// <summary>How many results a search returns.</summary>
    [Range(1, 100)]
    public int SearchLimit { get; set; } = 10;
}
