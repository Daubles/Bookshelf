using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Bookshelf.Models;

/// <summary>
/// The fields every Open Library record has. <see cref="Type"/> is e.g. <c>/type/work</c>,
/// <c>/type/redirect</c> (merged into the record at <see cref="Location"/>) or <c>/type/delete</c>.
/// </summary>
public interface IOpenLibraryRecord
{
    string Key { get; }
    OpenLibraryKeyRef? Type { get; }
    string? Location { get; }
}

/// <summary><c>GET /works/{key}.json</c></summary>
[UsedImplicitly]
public record OpenLibraryWork(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("type")] OpenLibraryKeyRef? Type,
    // Set when Type is /type/redirect: the key of the work this one was merged into.
    [property: JsonPropertyName("location")] string? Location,
    // Nullable because redirect and delete records come back from the same endpoint without one.
    // OpenLibraryClient.GetWorkAsync only returns real works with a title, so it's never null there.
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("subtitle")] string? Subtitle,
    [property: JsonPropertyName("description"), JsonConverter(typeof(OpenLibraryTextConverter))] string? Description,
    [property: JsonPropertyName("first_publish_date")] string? FirstPublishDate,
    [property: JsonPropertyName("covers")] int[]? Covers,
    [property: JsonPropertyName("authors")] OpenLibraryWorkAuthor[]? Authors) : IOpenLibraryRecord;

[UsedImplicitly]
public record OpenLibraryWorkAuthor(
    [property: JsonPropertyName("author")] OpenLibraryKeyRef? Author);

/// <summary><c>GET /works/{key}/editions.json</c></summary>
[UsedImplicitly]
public record OpenLibraryEditionsPage(
    [property: JsonPropertyName("size")] int Size,
    [property: JsonPropertyName("entries")] OpenLibraryEdition[] Entries);

[UsedImplicitly]
public record OpenLibraryEdition(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("publishers")] string[]? Publishers,
    [property: JsonPropertyName("publish_date")] string? PublishDate,
    [property: JsonPropertyName("number_of_pages")] int? NumberOfPages,
    [property: JsonPropertyName("languages")] OpenLibraryKeyRef[]? Languages,
    [property: JsonPropertyName("covers")] int[]? Covers);

/// <summary><c>GET /authors/{key}.json</c></summary>
[UsedImplicitly]
public record OpenLibraryAuthor(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("type")] OpenLibraryKeyRef? Type,
    // Set when Type is /type/redirect: the key of the author this one was merged into.
    [property: JsonPropertyName("location")] string? Location,
    // Nullable because redirect and delete records have none; OpenLibraryClient.GetAuthorAsync
    // only returns authors with a name.
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("bio"), JsonConverter(typeof(OpenLibraryTextConverter))] string? Bio,
    [property: JsonPropertyName("birth_date")] string? BirthDate,
    [property: JsonPropertyName("death_date")] string? DeathDate,
    [property: JsonPropertyName("photos")] int[]? Photos) : IOpenLibraryRecord;

/// <summary>A <c>{ "key": "/authors/OL123A" }</c> reference.</summary>
[UsedImplicitly]
public record OpenLibraryKeyRef(
    [property: JsonPropertyName("key")] string Key);

/// <summary>
/// Open Library text fields such as descriptions and bios are either a plain string
/// or <c>{ "type": "/type/text", "value": "..." }</c>. This reads both as a string.
/// </summary>
public class OpenLibraryTextConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.StartObject:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String
                        ? value.GetString()
                        : null;
                }
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
