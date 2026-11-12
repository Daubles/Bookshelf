using Bookshelf.Services;

namespace Bookshelf.Endpoints;

public static class BookEndpoints
{
    private const string EndpointGroup = "/api/book";
    private const int SearchLimit = 5;

    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var routeBuilder = app.MapGroup(EndpointGroup)
            .WithTags("Books");

        routeBuilder.MapGet("search/{query}", async (string query, BookCatalogService catalog, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await catalog.SearchAsync(query, SearchLimit, ct));
            }
            catch (HttpRequestException)
            {
                return OpenLibraryUnavailable("Search results couldn't be loaded from Open Library.");
            }
        });

        routeBuilder.MapGet("{workKey}", async (string workKey, BookCatalogService catalog, CancellationToken ct) =>
        {
            try
            {
                var work = await catalog.GetWorkAsync(workKey, ct);
                return work is null ? Results.NotFound() : Results.Ok(work);
            }
            catch (HttpRequestException)
            {
                return OpenLibraryUnavailable("The book couldn't be loaded from Open Library.");
            }
        });
    }

    private static IResult OpenLibraryUnavailable(string detail) =>
        Results.Problem(
            title: "Open Library is unavailable",
            detail: $"{detail} Please try again later.",
            statusCode: StatusCodes.Status502BadGateway);
}
