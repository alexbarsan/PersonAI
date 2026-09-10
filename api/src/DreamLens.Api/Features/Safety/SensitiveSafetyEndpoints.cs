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
            SensitiveSafetyRawAccessRequest request,
            [FromServices] GetSensitiveSafetyReviewRawHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, request, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { purpose = new[] { exception.Message } });
            }
        })
            .WithName("AccessSensitiveSafetyReviewRawText")
            .WithSummary("Explicitly accesses one encrypted dream snapshot and creates an audit record. Never use for routine review.");

        return app;
    }
}
