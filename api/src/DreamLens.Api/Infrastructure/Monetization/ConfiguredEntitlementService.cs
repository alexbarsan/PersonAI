using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Infrastructure.Monetization;

public sealed class ConfiguredEntitlementService(
    IOptions<MonetizationOptions> options,
    QuotaExemptionService quotaExemptionService,
    IServiceProvider serviceProvider) : IEntitlementService
{
    public EntitlementSnapshot GetEntitlement(string userSubject)
    {
        var value = options.Value;
        var premium = value.PremiumSubjects.Contains(userSubject, StringComparer.Ordinal)
            || serviceProvider.GetService<DreamLensDbContext>()?.PremiumGrants
                .Any(grant => grant.UserSubject == userSubject && grant.RevokedAt == null) == true;
        var quotaExempt = quotaExemptionService.IsExempt(userSubject);
        return premium
            ? new EntitlementSnapshot(EntitlementTier.Premium, value.PremiumDailyDreamSubmissions, true, quotaExempt)
            : new EntitlementSnapshot(EntitlementTier.Free, value.FreeDailyDreamSubmissions, false, quotaExempt);
    }
}
