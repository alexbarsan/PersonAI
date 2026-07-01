using System.Text.Json;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using PersonaKit.Context;
using PersonaKit.Pipeline;

namespace DreamLens.Api.Features.Dreams;

public sealed class SubmitDreamHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor,
    IInterpretationPipeline interpretationPipeline)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SubmitDreamResult> HandleAsync(
        SubmitDreamRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return SubmitDreamResult.Invalid(errors);
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);

        if (profile is null)
        {
            return SubmitDreamResult.Invalid(new Dictionary<string, string[]>
            {
                ["profile"] = ["Profile must be completed before submitting dreams."]
            });
        }

        if (!profile.ConsentAiProcessing)
        {
            return SubmitDreamResult.Invalid(new Dictionary<string, string[]>
            {
                ["consent"] = ["AI processing consent is required before submitting dreams."]
            });
        }

        var traits = JsonSerializer.Deserialize<ProfileTraitsDto>(encryptor.Decrypt(profile.EncryptedTraitsJson), JsonOptions)
            ?? ProfileTraitsDto.Empty;
        var dreamText = request.Text!.Trim();
        var interpretation = await interpretationPipeline.InterpretAsync(
            new InterpretationRequest(
                "dream-interpreter",
                new ContextBuildRequest(
                    Guid.NewGuid().ToString(),
                    NormalizeLocale(profile.Language),
                    new ContextPersona("dream-interpreter", "1.0.0"),
                    new ContextUserSource(
                        profile.UserSubject,
                        null,
                        null,
                        profile.Age,
                        profile.Sex,
                        profile.GenderIdentity,
                        profile.Language,
                        profile.Timezone,
                        new ContextTraits(
                            traits.Fears,
                            traits.Allergies,
                            traits.Interests,
                            traits.Occupation,
                            traits.RelationshipStatus,
                            traits.CulturalBackground,
                            traits.SleepPattern,
                            traits.StressLevel,
                            traits.RecentLifeEvents),
                        new ContextConsent(
                            profile.ConsentAiProcessing,
                            profile.ConsentSensitiveTraits,
                            profile.ConsentHistoryUse)),
                    null,
                    new DreamInput(
                        dreamText,
                        Normalize(request.Mood),
                        request.SleepQuality,
                        NormalizeArray(request.Tags),
                        Normalize(request.OccurredAt)))),
            cancellationToken);

        var result = interpretation.Result is null ? null : MapResult(interpretation.Result);
        var record = new DreamRecord
        {
            UserSubject = currentUser.Subject,
            Text = dreamText,
            Mood = Normalize(request.Mood),
            SleepQuality = request.SleepQuality,
            TagsJson = JsonSerializer.Serialize(NormalizeArray(request.Tags), JsonOptions),
            OccurredAt = Normalize(request.OccurredAt),
            Status = interpretation.Status == InterpretationStatus.Completed ? "completed" : "failed",
            ResultJson = result is null ? null : JsonSerializer.Serialize(result, JsonOptions),
            ErrorMessage = interpretation.ErrorMessage
        };

        dbContext.Dreams.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return SubmitDreamResult.Valid(DreamMapper.Map(record, result));
    }

    private static Dictionary<string, string[]> Validate(SubmitDreamRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Trim().Length < 10)
        {
            errors["text"] = ["Dream text must be at least 10 characters."];
        }

        if (request.SleepQuality is < 1 or > 5)
        {
            errors["sleepQuality"] = ["Sleep quality must be between 1 and 5."];
        }

        return errors;
    }

    private static DreamResultResponse MapResult(InterpretationResult result)
    {
        return new DreamResultResponse(
            result.Summary,
            result.Sections.Select(section => new DreamSectionResponse(section.Kind, section.Title, section.Content)).ToArray(),
            result.FollowUpQuestions);
    }

    private static string NormalizeLocale(string language)
    {
        return string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en-US" : language;
    }

    private static string[] NormalizeArray(string[]? values)
    {
        return values?
            .Select(Normalize)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToArray() ?? [];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
