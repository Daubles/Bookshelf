using System.Net.Http.Json;
using System.Text.Json;

namespace Bookshelf.Client.Authentication;

public static class ProblemErrors
{
    // Reads the messages from a ValidationProblem response; empty if the body has none or isn't JSON
    public static async Task<string[]> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>();
            return problem?.Errors?.SelectMany(e => e.Value).ToArray() ?? [];
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return [];
        }
    }

    private record Problem(Dictionary<string, string[]>? Errors);
}
