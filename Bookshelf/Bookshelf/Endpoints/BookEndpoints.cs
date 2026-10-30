using Bookshelf.Models;

namespace Bookshelf.Endpoints;

public static class BookEndpoints
{
    private const string EndpointGroup = "/api/book";
    private const string OpenLibraryUri = "https://openlibrary.org/search.json";

    public static void MapBookEndpoints(this IEndpointRouteBuilder app, HttpClient httpClient)
    {
        var routeBuilder = app.MapGroup(EndpointGroup);

        routeBuilder.MapGet("search/{query}", async (string query) =>
        {
            var olSearchResult = await httpClient.GetFromJsonAsync<OpenLibraryBookSearchResult>($"{OpenLibraryUri}?q={query}&limit=5");
            
            var searchResult = olSearchResult?.Docs
                .Select(book => new BookSummaryDto { OpenLibraryKey = book.Key, Title = book.Title, AuthorName = book.AuthorName, FirstPublishYear = book.FirstPublishYear })
                .ToList();
            
            return Results.Ok(searchResult);
        });
    }

}