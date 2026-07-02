using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Monetization;

namespace DreamLens.Api.Features.Entitlements;

public sealed class GetEntitlementHandler(
    ICurrentUser currentUser,
    IEntitlementService entitlementService)
{
    public EntitlementResponse Handle()
    {
        var entitlement = entitlementService.GetEntitlement(currentUser.Subject);
        return new EntitlementResponse(
            entitlement.Tier.ToString().ToLowerInvariant(),
            entitlement.DailyDreamLimit,
            entitlement.DeepAnalysisEnabled);
    }
}
