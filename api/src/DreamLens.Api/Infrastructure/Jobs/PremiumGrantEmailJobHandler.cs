using System.Text.Json;
using DreamLens.Api.Features.Entitlements;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class PremiumGrantEmailJobHandler(
    DreamLensDbContext dbContext,
    IPremiumGrantEmailSender sender) : IAsyncJobHandler
{
    public string JobType => AsyncJobTypes.PremiumGrantEmail;

    public async Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<PremiumGrantEmailJobPayload>(message.PayloadJson)
            ?? throw new InvalidOperationException("Premium grant email job payload is invalid.");
        var grant = await dbContext.PremiumGrants.SingleOrDefaultAsync(
            candidate => candidate.Id == payload.PremiumGrantId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Premium grant was not found for email delivery.");
        if (grant.RevokedAt is not null || grant.PremiumWelcomeEmailSentAt is not null)
        {
            return;
        }

        var name = await dbContext.UserProfiles
            .Where(profile => profile.UserSubject == message.UserSubject)
            .Select(profile => profile.PreferredName)
            .SingleOrDefaultAsync(cancellationToken);
        var recipientName = string.IsNullOrWhiteSpace(name) ? "there" : name.Trim();
        var delivery = await sender.SendAsync(
            new PremiumGrantWelcomeEmail(grant.EmailNormalized, recipientName),
            cancellationToken);
        grant.PremiumWelcomeEmailSentAt = DateTimeOffset.UtcNow;
        grant.PremiumWelcomeEmailProviderMessageId = delivery.ProviderMessageId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public sealed record PremiumGrantEmailJobPayload(Guid PremiumGrantId);
}
