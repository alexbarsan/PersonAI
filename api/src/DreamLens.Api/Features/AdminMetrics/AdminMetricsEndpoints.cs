using DreamLens.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.AdminMetrics;

public static class AdminMetricsEndpoints
{
    public static IEndpointRouteBuilder MapAdminMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/metrics", async (
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromServices] GetAdminMetricsHandler handler,
            CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await handler.HandleAsync(from, to, cancellationToken)); }
            catch (ArgumentException exception) { return Results.BadRequest(new { date = new[] { exception.Message } }); }
        })
            .RequireAuthorization(BusinessMetricsAuthorizationExtensions.BusinessMetricsAdminPolicy)
            .WithTags("Admin")
            .WithName("GetAdminMetrics")
            .WithSummary("Returns aggregated, non-PII product and AI cost metrics for business administrators.");

        return app;
    }
}
