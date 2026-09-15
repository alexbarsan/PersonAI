using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

internal static class DreamFactExtractor
{
    private const int MaxFactsPerType = 16;

    public static IReadOnlyCollection<DreamFactRecord> Extract(DreamRecord dream, string outputJson)
    {
        using var document = JsonDocument.Parse(outputJson);
        var root = document.RootElement;
        var schemaVersion = ReadString(root, "schemaVersion") ?? "unknown";
        var confidence = ReadDecimal(root, "factExtractionConfidence") ?? ReadDecimal(root, "confidence");
        var facts = new Dictionary<(string FactType, string NormalizedValue), DreamFactRecord>();

        AddStringArray("symbol", "symbols", root, dream, schemaVersion, confidence, facts, "symbol");
        AddEmotionFacts(root, dream, schemaVersion, confidence, facts);
        AddStringArray("theme", "themes", root, dream, schemaVersion, confidence, facts);
        AddNamedArray("person", "people", root, dream, schemaVersion, confidence, facts);
        AddNamedArray("location", "locations", root, dream, schemaVersion, confidence, facts);
        AddStringArray("object", "objects", root, dream, schemaVersion, confidence, facts);
        AddStringArray("scenario", "scenarios", root, dream, schemaVersion, confidence, facts);
        AddScore("lucidity-score", "lucidity", "lucidityScore", root, dream, schemaVersion, confidence, facts);
        AddScore("nightmare-intensity", "nightmare", "nightmareIntensity", root, dream, schemaVersion, confidence, facts);

        return facts.Values;
    }

    private static void AddStringArray(
        string factType,
        string propertyName,
        JsonElement root,
        DreamRecord dream,
        string schemaVersion,
        decimal? confidence,
        IDictionary<(string FactType, string NormalizedValue), DreamFactRecord> facts,
        string? objectPropertyName = null)
    {
        if (!root.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in values.EnumerateArray().Take(MaxFactsPerType))
        {
            var displayValue = item.ValueKind == JsonValueKind.String
                ? item.GetString()
                : objectPropertyName is not null && item.ValueKind == JsonValueKind.Object
                    ? ReadString(item, objectPropertyName)
                    : null;
            Add(factType, displayValue, null, propertyName + (objectPropertyName is null ? string.Empty : $".{objectPropertyName}"), dream, schemaVersion, confidence, facts);
        }
    }

    private static void AddEmotionFacts(
        JsonElement root,
        DreamRecord dream,
        string schemaVersion,
        decimal? confidence,
        IDictionary<(string FactType, string NormalizedValue), DreamFactRecord> facts)
    {
        if (!root.TryGetProperty("emotions", out var emotions) || emotions.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var emotion in emotions.EnumerateArray().Take(MaxFactsPerType))
        {
            Add("emotion", ReadString(emotion, "name"), ReadDecimal(emotion, "intensity"), "emotions.name", dream, schemaVersion, confidence, facts);
        }
    }

    private static void AddNamedArray(
        string factType,
        string propertyName,
        JsonElement root,
        DreamRecord dream,
        string schemaVersion,
        decimal? confidence,
        IDictionary<(string FactType, string NormalizedValue), DreamFactRecord> facts)
    {
        AddStringArray(factType, propertyName, root, dream, schemaVersion, confidence, facts, "name");
    }

    private static void AddScore(
        string factType,
        string displayValue,
        string propertyName,
        JsonElement root,
        DreamRecord dream,
        string schemaVersion,
        decimal? confidence,
        IDictionary<(string FactType, string NormalizedValue), DreamFactRecord> facts)
    {
        var score = ReadDecimal(root, propertyName);
        if (score is not null)
        {
            Add(factType, displayValue, score, propertyName, dream, schemaVersion, confidence, facts);
        }
    }

    private static void Add(
        string factType,
        string? displayValue,
        decimal? score,
        string sourceField,
        DreamRecord dream,
        string schemaVersion,
        decimal? confidence,
        IDictionary<(string FactType, string NormalizedValue), DreamFactRecord> facts)
    {
        if (string.IsNullOrWhiteSpace(displayValue))
        {
            return;
        }

        var normalizedValue = DreamFactNormalization.Normalize(displayValue);
        if (normalizedValue.Length == 0)
        {
            return;
        }

        var key = (factType, normalizedValue);
        var record = new DreamFactRecord
        {
            DreamId = dream.Id,
            UserSubject = dream.UserSubject,
            FactType = factType,
            NormalizedValue = normalizedValue,
            DisplayValue = displayValue.Trim(),
            Score = score,
            ExtractionConfidence = confidence,
            SourceSchemaVersion = schemaVersion,
            SourceField = sourceField,
            NormalizationVersion = DreamFactNormalization.Version
        };

        if (!facts.TryGetValue(key, out var existing) || (record.Score ?? 0) > (existing.Score ?? 0))
        {
            facts[key] = record;
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value)
            ? value
            : null;
    }
}
