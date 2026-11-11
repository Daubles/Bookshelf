using Bookshelf.Client.Models;
using Bookshelf.Data.Books;

namespace Bookshelf.Services;

/// <summary>Maps stored book entities to the DTOs sent to the client.</summary>
public static class BookMappings
{
    public static WorkDetailDto ToDetailDto(this Work work) => new()
    {
        OpenLibraryKey = work.OpenLibraryKey,
        Title = work.Title,
        Subtitle = work.Subtitle,
        Description = work.Description,
        FirstPublishYear = work.FirstPublishYear,
        CoverId = work.CoverId,
        Publisher = work.Publisher,
        NumberOfPages = work.NumberOfPages,
        Authors = work.Authors
            .OrderBy(wa => wa.Position)
            .Select(wa => new AuthorSummaryDto { OpenLibraryKey = wa.Author.OpenLibraryKey, Name = wa.Author.Name })
            .ToList(),
    };

}
