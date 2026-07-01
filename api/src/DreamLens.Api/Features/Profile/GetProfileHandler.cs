using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

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
            return new ProfileResponse(null, null, null, "en", "UTC", ProfileTraitsDto.Empty, ConsentDto.Empty);
        }

        return Map(profile, encryptor);
    }

    internal static ProfileResponse Map(UserProfile profile, IStringEncryptor encryptor)
    {
        var traitsJson = encryptor.Decrypt(profile.EncryptedTraitsJson);
        var traits = JsonSerializer.Deserialize<ProfileTraitsDto>(traitsJson) ?? ProfileTraitsDto.Empty;

        return new ProfileResponse(
            profile.Age,
            profile.Sex,
            profile.GenderIdentity,
            profile.Language,
            profile.Timezone,
            traits,
            new ConsentDto(
                profile.ConsentAiProcessing,
                profile.ConsentSensitiveTraits,
                profile.ConsentHistoryUse));
    }
}
