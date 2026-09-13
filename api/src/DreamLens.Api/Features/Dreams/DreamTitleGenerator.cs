using System.Text.Json;

namespace DreamLens.Api.Features.Dreams;

internal static class DreamTitleGenerator
{
    private const int MaxLength = 120;

    public static string FromInterpretation(string? rawJson, string? summary, string dreamText)
    {
        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            try
            {
                using var document = JsonDocument.Parse(rawJson);
                if (document.RootElement.TryGetProperty("title", out var title)
                    && title.ValueKind == JsonValueKind.String)
                {
                    return Create(title.GetString(), summary, dreamText);
                }
            }
            catch (JsonException)
            {
                // The interpretation pipeline already records invalid output; use the local fallback here.
            }
        }

        return Create(null, summary, dreamText);
    }

    public static string Create(string? storedOrGeneratedTitle, string? summary, string dreamText)
    {
        var title = Normalize(storedOrGeneratedTitle);
        if (!string.IsNullOrEmpty(title))
        {
            return title;
        }

        var fallback = FirstSentence(summary) ?? FirstSentence(dreamText) ?? "Untitled dream";
        return Normalize(fallback) ?? "Untitled dream";
    }

    private static string? FirstSentence(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sentenceEnd = value.IndexOfAny(['.', '!', '?', '\r', '\n']);
        return sentenceEnd >= 0 ? value[..sentenceEnd] : value;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= MaxLength ? compact : compact[..MaxLength].TrimEnd();
    }
}
