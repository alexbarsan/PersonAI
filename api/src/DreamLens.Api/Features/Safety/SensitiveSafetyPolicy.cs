using System.Text.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;
using PersonaKit.Context;

namespace DreamLens.Api.Features.Safety;

public sealed class SensitiveSafetyOptions
{
    public decimal ReviewConfidenceThreshold { get; set; } = 0.80m;

    public bool NotifyOnExplicitAdultSexualContent { get; set; }

    public int ReviewRetentionDays { get; set; } = 30;
}

public sealed record SensitiveSafetyCategory(string Category, decimal Confidence, string Severity);

public static class SensitiveSafetyCategories
{
    public const string SelfHarmOrSuicide = "self-harm-or-suicide";
    public const string ThreatsToOthers = "threats-to-others";
    public const string SexualViolenceOrCoercion = "sexual-violence-or-coercion";
    public const string PossibleMinorSexualContent = "possible-minor-sexual-content";
    public const string AbuseOrTrauma = "abuse-or-trauma";
    public const string ExplicitAdultSexualContent = "explicit-adult-sexual-content";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        SelfHarmOrSuicide,
        ThreatsToOthers,
        SexualViolenceOrCoercion,
        PossibleMinorSexualContent,
        AbuseOrTrauma,
        ExplicitAdultSexualContent
    };

    public static bool RestrictsElaboration(string category) => category is
        SelfHarmOrSuicide or ThreatsToOthers or SexualViolenceOrCoercion or PossibleMinorSexualContent;
}

public sealed class SensitiveSafetyEventFactory(
    IPseudonymService pseudonymService,
    IStringEncryptor encryptor,
    IOptions<SensitiveSafetyOptions> options)
{
    public IReadOnlyCollection<SensitiveDreamSafetyEvent> Create(
        DreamRecord dream,
        string rawDreamText,
        DreamSafetyResponse? safety)
    {
        if (safety?.Categories is not { Length: > 0 })
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        var configured = options.Value;
        var threshold = Math.Clamp(configured.ReviewConfidenceThreshold, 0m, 1m);
        var retention = Math.Clamp(configured.ReviewRetentionDays, 1, 365);
        var encryptedText = encryptor.Encrypt(rawDreamText);
        var pseudonym = pseudonymService.CreatePseudonym(dream.UserSubject);

        return safety.Categories
            .Where(category => SensitiveSafetyCategories.All.Contains(category.Category))
            .GroupBy(category => category.Category, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(category => category.Confidence).First())
            .Select(category =>
            {
                var reviewRequired = category.Confidence >= threshold
                    && (category.Category != SensitiveSafetyCategories.ExplicitAdultSexualContent
                        || configured.NotifyOnExplicitAdultSexualContent);
                return new SensitiveDreamSafetyEvent
                {
                    DreamId = dream.Id,
                    UserSubject = dream.UserSubject,
                    SubjectPseudonym = pseudonym,
                    Category = category.Category,
                    Confidence = category.Confidence,
                    Severity = category.Severity,
                    ReviewRequired = reviewRequired,
                    RestrictsElaboration = reviewRequired && SensitiveSafetyCategories.RestrictsElaboration(category.Category),
                    EncryptedDreamText = encryptedText,
                    DetectedAt = now,
                    ExpiresAt = now.AddDays(retention)
                };
            })
            .ToArray();
    }
}

public static class SensitiveSafetyParser
{
    public static DreamSafetyResponse Parse(JsonElement safetyElement, SensitiveSafetyOptions options)
    {
        var categories = safetyElement.TryGetProperty("categories", out var categoriesElement)
            && categoriesElement.ValueKind == JsonValueKind.Array
            ? categoriesElement.EnumerateArray()
                .Where(element => element.TryGetProperty("category", out _)
                    && element.TryGetProperty("confidence", out _)
                    && element.TryGetProperty("severity", out _))
                .Select(element => new SensitiveSafetyCategory(
                    element.GetProperty("category").GetString() ?? "",
                    decimal.Clamp(element.GetProperty("confidence").GetDecimal(), 0m, 1m),
                    element.GetProperty("severity").GetString() ?? "review"))
                .Where(category => SensitiveSafetyCategories.All.Contains(category.Category))
                .ToArray()
            : [];
        var threshold = Math.Clamp(options.ReviewConfidenceThreshold, 0m, 1m);
        var isRestricted = categories.Any(category => category.Confidence >= threshold
            && SensitiveSafetyCategories.RestrictsElaboration(category.Category));
        return new DreamSafetyResponse(
            safetyElement.GetProperty("selfHarmRisk").GetString() ?? "none",
            safetyElement.GetProperty("notes").GetString() ?? "",
            categories,
            isRestricted);
    }
}
