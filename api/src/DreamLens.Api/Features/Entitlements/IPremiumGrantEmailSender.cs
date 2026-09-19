namespace DreamLens.Api.Features.Entitlements;

public interface IPremiumGrantEmailSender
{
    Task<PremiumGrantEmailDelivery> SendAsync(PremiumGrantWelcomeEmail email, CancellationToken cancellationToken);
}

public sealed record PremiumGrantEmailDelivery(string ProviderMessageId);
