using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DreamLens.Api.Features.Profile;

public sealed class GetProfileHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor)
{
    public async Task<ProfileResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserSubject = currentUser.Subject,
                EmailNormalized = NormalizeEmail(currentUser.Email),
                EncryptedTraitsJson = encryptor.Encrypt(JsonSerializer.Serialize(ProfileTraitsDto.Empty))
            };
            dbContext.UserProfiles.Add(profile);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsSubjectUniqueViolation(exception))
            {
                dbContext.Entry(profile).State = EntityState.Detached;
                profile = await dbContext.UserProfiles
                    .AsNoTracking()
                    .SingleAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);
            }
        }

        return Map(profile, encryptor);
    }

    internal static ProfileResponse Map(UserProfile profile, IStringEncryptor encryptor)
    {
        var traitsJson = encryptor.Decrypt(profile.EncryptedTraitsJson);
        var traits = JsonSerializer.Deserialize<ProfileTraitsDto>(traitsJson) ?? ProfileTraitsDto.Empty;

        return new ProfileResponse(
            profile.PreferredName,
            profile.Age,
            profile.Sex,
            profile.Language,
            profile.Timezone,
            traits,
            new ConsentDto(
                profile.ConsentAiProcessing,
                profile.ConsentSensitiveTraits,
                profile.ConsentHistoryUse));
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        return normalized is not null && normalized.Length <= 320 ? normalized : null;
    }

    private static bool IsSubjectUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_UserProfiles_UserSubject"
        };
    }
}
