using System.Text.Json.Serialization;

namespace Bookshelf.Models;

public record OpenLibraryBookSearchResult(
    [property: JsonPropertyName("numFound")] int NumberFound,
    [property: JsonPropertyName("docs")] List<OpenLibraryDoc> Docs);

public record OpenLibraryDoc(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("author_name")] string AuthorName,
    [property: JsonPropertyName("first_publish_year")] string FirstPublishYear
    );