using System.Text.Json;

namespace PersonaKit.Pipeline;

public sealed class SectionMapResultMapper : IResultSectionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<InterpretationResult> MapAsync(
        Personas.PersonaDefinition persona,
        string outputJson,
        CancellationToken cancellationToken = default)
    {
        var sectionMapJson = await File.ReadAllTextAsync(persona.SectionMapPath, cancellationToken);
        var sectionMap = JsonSerializer.Deserialize<SectionMap>(sectionMapJson, JsonOptions)
            ?? throw new InvalidOperationException($"Section map '{persona.SectionMapPath}' is empty.");

        using var output = JsonDocument.Parse(outputJson);
        var root = output.RootElement;
        var sections = sectionMap.Sections
            .Select(section => MapSection(root, section))
            .ToArray();

        return new InterpretationResult(
            ReadString(root, sectionMap.Summary) ?? "",
            sections,
            ReadStringArray(root, sectionMap.FollowUpQuestions),
            outputJson);
    }

    private static InterpretationSection MapSection(JsonElement root, SectionMapItem item)
    {
        var source = Resolve(root, item.Source);
        object? content;

        if (item.Kind is "symbols" && source.ValueKind == JsonValueKind.Array)
        {
            content = source.EnumerateArray()
                .Select(symbol => new Dictionary<string, object?>
                {
                    ["title"] = item.TitleField is null ? null : GetStringProperty(symbol, item.TitleField),
                    ["body"] = item.BodyFields.Select(field => GetStringProperty(symbol, field)).Where(value => value is not null).ToArray()
                })
                .ToArray();
        }
        else if (item.Kind is "emotions" && source.ValueKind == JsonValueKind.Array)
        {
            content = source.EnumerateArray()
                .Select(emotion => new Dictionary<string, object?>
                {
                    ["title"] = item.TitleField is null ? null : GetStringProperty(emotion, item.TitleField),
                    ["body"] = item.BodyField is null ? null : GetStringProperty(emotion, item.BodyField),
                    ["value"] = item.ValueField is null ? null : GetNumberProperty(emotion, item.ValueField)
                })
                .ToArray();
        }
        else if (item.Kind is "list")
        {
            content = source.ValueKind == JsonValueKind.Array
                ? source.EnumerateArray().Select(value => value.GetString()).Where(value => value is not null).ToArray()
                : [];
        }
        else
        {
            content = source.ValueKind == JsonValueKind.String ? source.GetString() : source.GetRawText();
        }

        return new InterpretationSection(item.Kind, item.Title, content);
    }

    private static string? ReadString(JsonElement root, string path)
    {
        var element = Resolve(root, path);
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private static string[] ReadStringArray(JsonElement root, string path)
    {
        var element = Resolve(root, path);
        return element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().Select(value => value.GetString()).Where(value => value is not null).Cast<string>().ToArray()
            : [];
    }

    private static JsonElement Resolve(JsonElement root, string path)
    {
        if (!path.StartsWith("$.", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported section map path '{path}'.");
        }

        var current = root;
        foreach (var segment in path[2..].Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current.GetProperty(segment);
        }

        return current;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static double? GetNumberProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetDouble()
            : null;
    }

    private sealed record SectionMap(string Summary, SectionMapItem[] Sections, string FollowUpQuestions);

    private sealed record SectionMapItem(
        string Kind,
        string Title,
        string Source,
        string? TitleField,
        string? BodyField,
        string[] BodyFields,
        string? ValueField)
    {
        public string[] BodyFields { get; init; } = BodyFields ?? [];
    }
}
