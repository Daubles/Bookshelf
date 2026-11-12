using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Bookshelf.Models;

/// <summary><c>GET /search.json</c></summary>
public record OpenLibraryBookSearchResult(
    [property: JsonPropertyName("numFound")] int NumberFound,
    [property: JsonPropertyName("docs")] List<OpenLibraryDoc> Docs);

[UsedImplicitly]
public record OpenLibraryDoc(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("author_name")] string[]? AuthorName,
    [property: JsonPropertyName("first_publish_year")] int? FirstPublishYear);
