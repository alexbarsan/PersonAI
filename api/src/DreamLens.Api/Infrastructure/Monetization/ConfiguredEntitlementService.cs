using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Monetization;

public sealed class ConfiguredEntitlementService(
    IOptions<MonetizationOptions> options,
    QuotaExemptionService quotaExemptionService) : IEntitlementService
{
    public EntitlementSnapshot GetEntitlement(string userSubject)
    {
        var value = options.Value;
        var premium = value.PremiumSubjects.Contains(userSubject, StringComparer.Ordinal);
        var quotaExempt = quotaExemptionService.IsExempt(userSubject);
        return premium
            ? new EntitlementSnapshot(EntitlementTier.Premium, value.PremiumDailyDreamSubmissions, true, quotaExempt)
            : new EntitlementSnapshot(EntitlementTier.Free, value.FreeDailyDreamSubmissions, false, quotaExempt);
    }
}
