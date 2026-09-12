using DreamLens.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.AdminOperations;

public static class AdminOperationsEndpoints
{
    public static IEndpointRouteBuilder MapAdminOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/operations", async (
            [FromServices] GetAdminOperationsHandler handler,
            CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(cancellationToken)))
            .RequireAuthorization(BusinessMetricsAuthorizationExtensions.BusinessMetricsAdminPolicy)
            .WithTags("Admin")
            .WithName("GetAdminOperations")
            .WithSummary("Returns queue, workload, failure, cost, and latency health for application administrators.");

        app.MapPost("/v1/admin/operations/jobs/{id:guid}/requeue", async (
            Guid id,
            [FromBody] AdminOperationsActionRequest request,
            [FromServices] RequeueAdminJobHandler handler,
            CancellationToken cancellationToken) => ToResult(await handler.HandleAsync(id, request, cancellationToken)))
            .RequireAuthorization(BusinessMetricsAuthorizationExtensions.BusinessMetricsAdminPolicy)
            .WithTags("Admin")
            .WithName("RequeueAdminJob")
            .WithSummary("Requeues a failed or stale job and records an administrator audit entry.");

        app.MapPost("/v1/admin/operations/issues/{source}/{id:guid}/acknowledge", async (
            string source,
            Guid id,
            [FromBody] AdminOperationsActionRequest request,
            [FromServices] AcknowledgeAdminIssueHandler handler,
            CancellationToken cancellationToken) => ToResult(await handler.HandleAsync(source, id, request, cancellationToken)))
            .RequireAuthorization(BusinessMetricsAuthorizationExtensions.BusinessMetricsAdminPolicy)
            .WithTags("Admin")
            .WithName("AcknowledgeAdminIssue")
            .WithSummary("Acknowledges an operational issue and records an administrator audit entry.");

        app.MapGet("/v1/admin/dreams", async (
            [FromQuery] string? query,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromServices] SearchAdminDreamsHandler handler,
            CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(
                query, status, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, cancellationToken)))
            .RequireAuthorization(PrivacyAuthorizationExtensions.PrivacyAdminPolicy)
            .WithTags("Admin")
            .WithName("SearchAdminDreams")
            .WithSummary("Searches private dreams and returns bounded metadata for privacy administrators.");

        app.MapPost("/v1/admin/dreams/{id:guid}/access", async (
            Guid id,
            [FromBody] AdminDreamAccessRequest request,
            [FromServices] AccessAdminDreamHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, request, cancellationToken);
            return result.StatusCode switch
            {
                200 => Results.Ok(result.Response),
                400 => Results.BadRequest(result.Errors),
                _ => Results.NotFound()
            };
        })
            .RequireAuthorization(PrivacyAuthorizationExtensions.PrivacyAdminPolicy)
            .WithTags("Admin")
            .WithName("AccessAdminDream")
            .WithSummary("Returns original dream content, interpretations, and generated images after auditing the administrator purpose.");

        return app;
    }

    private static IResult ToResult(AdminOperationsActionResult result) => result.StatusCode switch
    {
        200 => Results.Ok(result.Response),
        400 => Results.BadRequest(result.Errors),
        404 => Results.NotFound(),
        409 => Results.Conflict(result.Errors),
        _ => Results.StatusCode(result.StatusCode)
    };
}
