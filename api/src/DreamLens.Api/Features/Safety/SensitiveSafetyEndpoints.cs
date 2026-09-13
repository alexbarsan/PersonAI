using DreamLens.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Safety;

public static class SensitiveSafetyEndpoints
{
    public static IEndpointRouteBuilder MapSensitiveSafetyEndpoints(this IEndpointRouteBuilder app)
    {
        var reviews = app.MapGroup("/v1/safety/admin/reviews")
            .RequireAuthorization(PrivacyAuthorizationExtensions.PrivacyAdminPolicy)
            .WithTags("Sensitive safety review");

        reviews.MapGet("", async ([FromQuery] string? status, [FromServices] ListSensitiveSafetyReviewsHandler handler, CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(status, cancellationToken)))
            .WithName("ListSensitiveSafetyReviews")
            .WithSummary("Lists privacy-preserving sensitive safety review metadata. Dream text is never included.");

        reviews.MapPost("{id:guid}/acknowledge", async (Guid id, [FromServices] AcknowledgeSensitiveSafetyReviewHandler handler, CancellationToken cancellationToken) =>
        {
            var review = await handler.HandleAsync(id, cancellationToken);
            return review is null ? Results.NotFound() : Results.Ok(review);
        })
            .WithName("AcknowledgeSensitiveSafetyReview")
            .WithSummary("Acknowledges a sensitive safety review event without accessing dream text.");

        reviews.MapPost("{id:guid}/raw-access", async (
            Guid id,
            [FromServices] GetSensitiveSafetyReviewRawHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
            .WithName("AccessSensitiveSafetyReviewRawText")
            .WithSummary("Accesses one encrypted dream snapshot for a privacy administrator and creates an automatic audit record.");

        return app;
    }
}
