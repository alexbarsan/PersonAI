using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Insights;

public static class InsightsEndpoints
{
    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/insights")
            .RequireAuthorization()
            .WithTags("Insights");

        group.MapGet("", async (
            [FromServices] GetInsightsHandler handler,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("GetInsights")
            .WithSummary("Returns simple dream insights for the current user.");

        return app;
    }
}
