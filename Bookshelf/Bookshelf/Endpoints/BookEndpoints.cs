using Bookshelf.Client.Models;
using Bookshelf.Models;

namespace Bookshelf.Endpoints;

public static class BookEndpoints
{
    private const string EndpointGroup = "/api/book";
    private const string OpenLibraryUri = "https://openlibrary.org/search.json";
    
    private static readonly HttpClient HttpClient = new();

    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var routeBuilder = app.MapGroup(EndpointGroup)
            .WithTags("Books");

        routeBuilder.MapGet("search/{query}", async (string query) =>
        {
            var olSearchResult = await HttpClient.GetFromJsonAsync<OpenLibraryBookSearchResult>($"{OpenLibraryUri}?q={Uri.EscapeDataString(query)}&limit=5");
            
            var searchResult = olSearchResult?.Docs
                .Select(book => new BookSummaryDto { OpenLibraryKey = book.Key, Title = book.Title, AuthorName = book.AuthorName, FirstPublishYear = book.FirstPublishYear })
                .ToList();
            
            return Results.Ok(searchResult);
        });
    }

}