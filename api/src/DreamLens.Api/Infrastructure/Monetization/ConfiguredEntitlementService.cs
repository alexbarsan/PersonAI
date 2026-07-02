using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Monetization;

public sealed class ConfiguredEntitlementService(IOptions<MonetizationOptions> options) : IEntitlementService
{
    public EntitlementSnapshot GetEntitlement(string userSubject)
    {
        var value = options.Value;
        var premium = value.PremiumSubjects.Contains(userSubject, StringComparer.Ordinal);
        return premium
            ? new EntitlementSnapshot(EntitlementTier.Premium, value.PremiumDailyDreamSubmissions, true)
            : new EntitlementSnapshot(EntitlementTier.Free, value.FreeDailyDreamSubmissions, false);
    }
}
