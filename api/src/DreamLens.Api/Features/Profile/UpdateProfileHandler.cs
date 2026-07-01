using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Profile;

public sealed class UpdateProfileHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor)
{
    public async Task<UpdateProfileResult> HandleAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return UpdateProfileResult.Invalid(errors);
        }

        var profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserSubject = currentUser.Subject,
                EncryptedTraitsJson = encryptor.Encrypt(JsonSerializer.Serialize(ProfileTraitsDto.Empty))
            };
            dbContext.UserProfiles.Add(profile);
        }

        profile.Age = request.Age;
        profile.Sex = Normalize(request.Sex);
        profile.GenderIdentity = Normalize(request.GenderIdentity);
        profile.Language = NormalizeRequired(request.Language);
        profile.Timezone = NormalizeRequired(request.Timezone);
        profile.EncryptedTraitsJson = encryptor.Encrypt(JsonSerializer.Serialize(NormalizeTraits(request.Traits!)));
        profile.ConsentAiProcessing = request.Consent!.AiProcessing;
        profile.ConsentSensitiveTraits = request.Consent.SensitiveTraits;
        profile.ConsentHistoryUse = request.Consent.HistoryUse;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return UpdateProfileResult.Valid(GetProfileHandler.Map(profile, encryptor));
    }

    private static Dictionary<string, string[]> Validate(UpdateProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (request.Age is < 13 or > 120)
        {
            errors["age"] = ["Age must be between 13 and 120."];
        }

        if (string.IsNullOrWhiteSpace(request.Language))
        {
            errors["language"] = ["Language is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Timezone))
        {
            errors["timezone"] = ["Timezone is required."];
        }

        if (request.Traits is null)
        {
            errors["traits"] = ["Traits are required."];
        }
        else
        {
            AddLengthErrors(errors, "occupation", request.Traits.Occupation, 128);
            AddLengthErrors(errors, "relationshipStatus", request.Traits.RelationshipStatus, 128);
            AddLengthErrors(errors, "culturalBackground", request.Traits.CulturalBackground, 256);
            AddLengthErrors(errors, "sleepPattern", request.Traits.SleepPattern, 256);
            AddLengthErrors(errors, "stressLevel", request.Traits.StressLevel, 64);
        }

        if (request.Consent is null)
        {
            errors["consent"] = ["Consent is required."];
        }

        AddLengthErrors(errors, "sex", request.Sex, 64);
        AddLengthErrors(errors, "genderIdentity", request.GenderIdentity, 128);

        return errors;
    }

    private static void AddLengthErrors(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (value?.Length > maxLength)
        {
            errors[key] = [$"{key} must be {maxLength} characters or fewer."];
        }
    }

    private static ProfileTraitsDto NormalizeTraits(ProfileTraitsDto traits)
    {
        return traits with
        {
            Fears = NormalizeArray(traits.Fears),
            Allergies = NormalizeArray(traits.Allergies),
            Interests = NormalizeArray(traits.Interests),
            Occupation = Normalize(traits.Occupation),
            RelationshipStatus = Normalize(traits.RelationshipStatus),
            CulturalBackground = Normalize(traits.CulturalBackground),
            SleepPattern = Normalize(traits.SleepPattern),
            StressLevel = Normalize(traits.StressLevel),
            RecentLifeEvents = NormalizeArray(traits.RecentLifeEvents)
        };
    }

    private static string[] NormalizeArray(string[] values)
    {
        return values
            .Select(Normalize)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToArray();
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
