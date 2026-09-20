using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Insights;

public static class InsightsEndpoints
{
    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/insights")
            .RequireAuthorization()
            .WithTags("Insights");

        group.MapGet("/observations", async (
            [FromQuery] string type,
            [FromQuery] string value,
            [FromServices] GetDreamObservationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var observation = await handler.HandleAsync(type, value, cancellationToken);
            return observation is null ? Results.NotFound() : Results.Ok(observation);
        })
            .WithName("GetDreamObservation")
            .WithSummary("Returns owner-scoped evidence, journal-derived related patterns, a persisted personalized reflection, research lenses, and monthly history for one map observation.");

        group.MapGet("", async (
            [FromServices] GetInsightsHandler handler,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("GetInsights")
            .WithSummary("Returns simple dream insights for the current user.");

        return app;
    }
}
