using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonaKit.Context;

public sealed class ContextBuilder(IPseudonymService pseudonymService) : IContextBuilder
{
    private const int MinimumDreamTextLength = 10;
    private const int MaximumDreamTextLength = 4_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public Task<string> BuildAsync(ContextBuildRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var context = new ContextDocument(
            "1.0",
            request.RequestId,
            request.Locale,
            request.Persona,
            BuildUser(request.User),
            request.User.Consent.HistoryUse ? request.History : null,
            BuildInput(request.Input));

        return Task.FromResult(JsonSerializer.Serialize(context, JsonOptions));
    }

    private ContextUser BuildUser(ContextUserSource source)
    {
        return new ContextUser(
            pseudonymService.CreatePseudonym(source.InternalUserId),
            source.Age,
            source.Sex,
            source.GenderIdentity,
            source.Language,
            source.Timezone,
            BuildTraits(source.Traits, source.Consent.SensitiveTraits),
            source.Consent);
    }

    private static object BuildTraits(ContextTraits traits, bool includeSensitiveTraits)
    {
        if (includeSensitiveTraits)
        {
            return traits;
        }

        return new ReducedContextTraits(
            traits.Interests,
            traits.Occupation,
            traits.RelationshipStatus,
            traits.SleepPattern,
            traits.StressLevel);
    }

    private static ContextDreamInput BuildInput(DreamInput input)
    {
        return new ContextDreamInput(
            "dream",
            input.Text.Length > MaximumDreamTextLength ? input.Text[..MaximumDreamTextLength] : input.Text,
            true,
            input.Mood,
            input.SleepQuality,
            input.Tags,
            input.OccurredAt);
    }

    private static void Validate(ContextBuildRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            throw new ContextValidationException("Request id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Locale))
        {
            throw new ContextValidationException("Locale is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Persona.Id) || string.IsNullOrWhiteSpace(request.Persona.Version))
        {
            throw new ContextValidationException("Persona id and version are required.");
        }

        if (string.IsNullOrWhiteSpace(request.User.InternalUserId))
        {
            throw new ContextValidationException("Internal user id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Input.Text) || request.Input.Text.Trim().Length < MinimumDreamTextLength)
        {
            throw new ContextValidationException("Dream text must be at least 10 characters.");
        }
    }

    private sealed record ContextDocument(
        string SchemaVersion,
        string RequestId,
        string Locale,
        ContextPersona Persona,
        ContextUser User,
        ContextHistory? History,
        ContextDreamInput Input);

    private sealed record ContextUser(
        string PseudonymId,
        int? Age,
        string? Sex,
        string? GenderIdentity,
        string Language,
        string Timezone,
        object Traits,
        ContextConsent Consent);

    private sealed record ReducedContextTraits(
        string[] Interests,
        string? Occupation,
        string? RelationshipStatus,
        string? SleepPattern,
        string? StressLevel);

    private sealed record ContextDreamInput(
        string Type,
        string Text,
        bool IsUntrusted,
        string? Mood,
        int? SleepQuality,
        string[] Tags,
        string? OccurredAt);
}
