using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Monetization;
using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Privacy;

public static class PrivacyEndpoints
{
    public static IEndpointRouteBuilder MapPrivacyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/privacy")
            .RequireAuthorization()
            .WithTags("Privacy");

        group.MapGet("/export", async (
            [FromServices] ExportUserDataHandler handler,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IEntitlementService entitlementService,
            CancellationToken cancellationToken) =>
        {
            if (!entitlementService.GetEntitlement(currentUser.Subject).DeepAnalysisEnabled)
            {
                return Results.Json(new { export = new[] { "Data export requires Premium access." } }, statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(await handler.HandleAsync(cancellationToken));
        })
            .WithName("ExportUserData")
            .WithSummary("Exports all currently accessible data for a Premium user, including voice transcripts and explicitly retained recordings.");

        group.MapPost("/anonymization-requests", async ([FromServices] RequestAnonymizationHandler handler, CancellationToken cancellationToken) =>
                Results.Accepted("/v1/privacy/anonymization-requests/me", await handler.HandleAsync(cancellationToken)))
            .WithName("RequestAnonymization")
            .WithSummary("Requests administrator-approved irreversible anonymization.");

        group.MapGet("/anonymization-requests/me", async ([FromServices] GetAnonymizationRequestHandler handler, CancellationToken cancellationToken) =>
        {
            var request = await handler.HandleAsync(cancellationToken);
            return request is null ? Results.NotFound() : Results.Ok(request);
        })
            .WithName("GetMyAnonymizationRequest")
            .WithSummary("Returns the current user's latest pending anonymization request.");

        var admin = group.MapGroup("/admin/anonymization-requests")
            .RequireAuthorization(PrivacyAuthorizationExtensions.PrivacyAdminPolicy);

        admin.MapGet("", async ([FromQuery] string? status, [FromServices] ListAnonymizationRequestsHandler handler, CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(status, cancellationToken)))
            .WithName("ListAnonymizationRequests")
            .WithSummary("Lists anonymization requests for a privacy administrator.");

        admin.MapPost("{id:guid}/approve", async (Guid id, [FromServices] ApproveAnonymizationHandler handler, CancellationToken cancellationToken) =>
        {
            var request = await handler.HandleAsync(id, cancellationToken);
            return request is null ? Results.NotFound() : Results.Ok(request);
        })
            .WithName("ApproveAnonymization")
            .WithSummary("Approves and completes irreversible user-data anonymization.");

        return app;
    }
}
