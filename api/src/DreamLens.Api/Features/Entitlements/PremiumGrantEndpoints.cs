using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Entitlements;

public static class PremiumGrantEndpoints
{
    public static IEndpointRouteBuilder MapPremiumGrantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/premium-grants")
            .RequireAuthorization()
            .WithTags("Admin");

        group.MapGet("", async ([FromServices] PremiumGrantHandler handler, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await handler.ListAsync(cancellationToken)); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        group.MapPost("", async (PremiumGrantRequest request, [FromServices] PremiumGrantHandler handler, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.GrantAsync(request, cancellationToken);
                return result.StatusCode switch
                {
                    StatusCodes.Status200OK => Results.Ok(result.Grant),
                    StatusCodes.Status404NotFound => Results.NotFound(new { message = result.Error }),
                    _ => Results.BadRequest(new { message = result.Error })
                };
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] PremiumGrantHandler handler, CancellationToken cancellationToken) =>
        {
            try { return await handler.RevokeAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        return app;
    }
}
