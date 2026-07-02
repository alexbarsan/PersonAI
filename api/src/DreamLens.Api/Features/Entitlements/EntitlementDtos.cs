namespace DreamLens.Api.Features.Entitlements;

public sealed record EntitlementResponse(
    string Tier,
    int DailyDreamLimit,
    bool DeepAnalysisEnabled);
