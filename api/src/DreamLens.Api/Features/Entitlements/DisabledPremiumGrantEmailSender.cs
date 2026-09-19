namespace DreamLens.Api.Features.Entitlements;

public sealed class DisabledPremiumGrantEmailSender : IPremiumGrantEmailSender
{
    public Task<PremiumGrantEmailDelivery> SendAsync(PremiumGrantWelcomeEmail email, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Premium grant email delivery is disabled.");
}
