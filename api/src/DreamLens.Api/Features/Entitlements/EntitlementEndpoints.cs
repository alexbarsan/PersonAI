namespace DreamLens.Api.Features.Entitlements;

public static class EntitlementEndpoints
{
    public static IEndpointRouteBuilder MapEntitlementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/entitlements", (GetEntitlementHandler handler) => Results.Ok(handler.Handle()))
            .RequireAuthorization()
            .WithName("GetEntitlements")
            .WithTags("Entitlements")
            .WithSummary("Returns the current user's monetization entitlement state.");

        return app;
    }
}
