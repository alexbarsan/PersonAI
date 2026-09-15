using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Features.Dreams;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Entitlements;

public sealed class GetEntitlementHandler(
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    DreamLensDbContext? dbContext = null,
    IAskQuotaService? askQuotaService = null)
{
    public async Task<EntitlementResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var entitlement = entitlementService.GetEntitlement(currentUser.Subject);
        var timezone = dbContext is null
            ? "UTC"
            : await dbContext.UserProfiles.AsNoTracking()
                .Where(profile => profile.UserSubject == currentUser.Subject)
                .Select(profile => profile.Timezone)
                .SingleOrDefaultAsync(cancellationToken) ?? "UTC";
        var ask = askQuotaService is null
            ? new AskQuotaState(0, 0, null, false)
            : await askQuotaService.GetStateAsync(currentUser.Subject, timezone, entitlement, cancellationToken);
        return new EntitlementResponse(
            entitlement.Tier.ToString().ToLowerInvariant(),
            entitlement.DailyDreamLimit,
            entitlement.DeepAnalysisEnabled,
            entitlement.QuotaExempt,
            ask.DailyLimit,
            ask.Remaining,
            ask.ResetsAt,
            ask.IsExempt);
    }
}
