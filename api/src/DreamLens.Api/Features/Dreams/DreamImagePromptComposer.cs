using System.Text.RegularExpressions;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

public sealed record DreamImagePrompt(string Version, string Text, DreamImagePromptMode Mode, string[] ModerationCategories);

public interface IDreamImagePromptComposer
{
    DreamImagePrompt Compose(DreamRecord dream, IReadOnlyCollection<DreamFactRecord> facts, ProfileTraitsDto traits, bool includeProfileContext, bool includeSensitiveTraits, ImagePromptSafetyResult safety, string style, string version);
}

public sealed class DreamImagePromptComposer : IDreamImagePromptComposer
{
    private const int MaxPromptLength = 700;
    private static readonly Regex UnsafePromptValue = new("(?:\\d{3}[- .]?\\d{2,}|@|https?://|\\b(?:nude|naked|sex|sexual|grop|rape|kill|murder|blood|weapon|gun|knife|suicide|self-harm)\\w*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly string[] WorkMarkers = ["work", "office", "meeting", "colleague", "boss", "job", "hospital", "school"];
    private static readonly string[] RelationshipMarkers = ["partner", "spouse", "husband", "wife", "boyfriend", "girlfriend", "family", "mother", "father"];
    private static readonly string[] AnxiousEmotions = ["anxiety", "anxious", "fear", "afraid", "stress", "panic", "worry"];

    public DreamImagePrompt Compose(DreamRecord dream, IReadOnlyCollection<DreamFactRecord> facts, ProfileTraitsDto traits, bool includeProfileContext, bool includeSensitiveTraits, ImagePromptSafetyResult safety, string style, string version)
    {
        var metadata = BuildMetadata(dream, facts, traits, includeProfileContext, includeSensitiveTraits);
        var prompt = safety.Mode == DreamImagePromptMode.Symbolic
            ? ComposeSymbolic(metadata, style)
            : ComposeStandard(metadata, style);
        return new DreamImagePrompt(version, prompt.Length <= MaxPromptLength ? prompt : prompt[..MaxPromptLength], safety.Mode, safety.Categories.ToArray());
    }

    private static PromptMetadata BuildMetadata(DreamRecord dream, IReadOnlyCollection<DreamFactRecord> facts, ProfileTraitsDto traits, bool includeProfileContext, bool includeSensitiveTraits)
    {
        var safeFacts = facts.Where(fact => !UnsafePromptValue.IsMatch(fact.DisplayValue)).ToArray();
        string[] locations = facts.Count(fact => string.Equals(fact.FactType, "location", StringComparison.OrdinalIgnoreCase)) switch
        {
            0 => [],
            1 => ["a familiar place"],
            _ => ["a shifting set of familiar places"]
        };
        return new PromptMetadata(
            Values(safeFacts, "object", 3),
            Values(safeFacts, "symbol", 3),
            Values(safeFacts, "theme", 2),
            locations,
            facts.Count(fact => string.Equals(fact.FactType, "person", StringComparison.OrdinalIgnoreCase)),
            includeProfileContext ? BuildRelevantUserContext(dream, safeFacts, traits, includeSensitiveTraits) : []);
    }

    private static string ComposeStandard(PromptMetadata metadata, string style)
    {
        var details = new List<string>();
        AddList(details, "Motifs", metadata.Objects.Concat(metadata.Symbols));
        AddList(details, "Themes", metadata.Themes);
        AddList(details, "Setting", metadata.Locations);
        if (metadata.People > 0)
        {
            details.Add($"Characters: {metadata.People} unnamed familiar figure{(metadata.People == 1 ? string.Empty : "s")}");
        }
        AddList(details, "Relevant context", metadata.UserContext);
        return $"A reflective dream scene in {DescribeStyle(style)}. {string.Join(". ", details)}. Dreamlike composition, no text or letters, no identifiable real people, no explicit sexual content, graphic violence, injuries, or weapons.";
    }

    private static string ComposeSymbolic(PromptMetadata metadata, string style)
    {
        var details = new List<string>();
        AddList(details, "Safe motifs", metadata.Objects.Concat(metadata.Symbols));
        AddList(details, "Atmosphere", metadata.Themes);
        AddList(details, "Setting", metadata.Locations);
        AddList(details, "Relevant context", metadata.UserContext);
        return $"An abstract, symbolic dream scene in {DescribeStyle(style)}. Convey emotion through light, weather, scale, architecture, water, and nature. {string.Join(". ", details)}. Use metaphor rather than literal events. No people, bodies, intimacy, threat, injury, weapons, gore, text, letters, or recognizable real-world identities.";
    }

    private static string[] BuildRelevantUserContext(DreamRecord dream, IReadOnlyCollection<DreamFactRecord> facts, ProfileTraitsDto traits, bool includeSensitiveTraits)
    {
        var corpus = string.Join(' ', facts.Select(fact => $"{fact.NormalizedValue} {fact.DisplayValue}").Append(dream.Text)).ToLowerInvariant();
        var context = new List<string>();
        if (!string.IsNullOrWhiteSpace(traits.Occupation) && ContainsAny(corpus, WorkMarkers)) context.Add("work-life context");
        if (!string.IsNullOrWhiteSpace(traits.RelationshipStatus) && ContainsAny(corpus, RelationshipMarkers)) context.Add("relationship context");
        if (includeSensitiveTraits && !string.IsNullOrWhiteSpace(traits.StressLevel) && ContainsAny(corpus, AnxiousEmotions)) context.Add("current emotional pressure");
        if (includeSensitiveTraits && traits.Fears.Any(fear => IsDirectlyRelevant(fear, corpus))) context.Add("a personal fear theme");
        if (includeSensitiveTraits && traits.RecentLifeEvents.Any(eventName => IsDirectlyRelevant(eventName, corpus))) context.Add("a current life transition");
        context.AddRange(traits.Interests.Where(interest => IsDirectlyRelevant(interest, corpus)).Take(2).Select(interest => $"interest: {Sanitize(interest)}"));
        return context.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToArray();
    }

    private static bool IsDirectlyRelevant(string value, string corpus)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Length >= 3 && !UnsafePromptValue.IsMatch(normalized) && corpus.Contains(normalized, StringComparison.Ordinal);
    }

    private static bool ContainsAny(string corpus, IEnumerable<string> values) => values.Any(value => corpus.Contains(value, StringComparison.Ordinal));

    private static string[] Values(IEnumerable<DreamFactRecord> facts, string type, int max) => facts.Where(fact => string.Equals(fact.FactType, type, StringComparison.OrdinalIgnoreCase)).Select(fact => Sanitize(fact.DisplayValue)).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(max).ToArray();

    private static string Sanitize(string value)
    {
        var normalized = string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim('.', ',', ';', ':');
        return normalized.Length > 80 ? normalized[..80] : normalized;
    }

    private static void AddList(List<string> target, string label, IEnumerable<string> values)
    {
        var list = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (list.Length > 0) target.Add($"{label}: {string.Join(", ", list)}");
    }

    private sealed record PromptMetadata(string[] Objects, string[] Symbols, string[] Themes, string[] Locations, int People, string[] UserContext);

    private static string DescribeStyle(string style) => style switch
    {
        "3D_ANIMATED_FAMILY_FILM" => "a warm, gentle 3D animated family-film style",
        "DESIGN_SKETCH" => "an expressive hand-drawn design sketch style",
        "FLAT_VECTOR_ILLUSTRATION" => "a clear flat vector illustration style",
        "GRAPHIC_NOVEL_ILLUSTRATION" => "an atmospheric graphic novel illustration style",
        "MAXIMALISM" => "a rich, layered maximalist illustration style",
        "MIDCENTURY_RETRO" => "a restrained midcentury retro illustration style",
        "PHOTOREALISM" => "a cinematic photorealistic style",
        _ => "a soft digital painting style"
    };
}
