using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Dreams;

public static class DreamEndpoints
{
    public static IEndpointRouteBuilder MapDreamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/dreams")
            .RequireAuthorization()
            .WithTags("Dreams");

        group.MapGet("", async (
            [FromServices] ListDreamsHandler handler,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("ListDreams")
            .WithSummary("Lists dreams for the current user.");

        group.MapPost("", async (
            SubmitDreamRequest request,
            [FromServices] SubmitDreamHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                return Results.BadRequest(result.Errors);
            }

            return result.IsCompleted
                ? Results.Ok(result.Dream)
                : Results.Json(result.Dream, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
            .WithName("SubmitDream")
            .WithSummary("Submits a dream and returns an interpretation result.");

        group.MapGet("{id:guid}", async (
            Guid id,
            [FromServices] GetDreamHandler handler,
            CancellationToken cancellationToken) =>
        {
            var dream = await handler.HandleAsync(id, cancellationToken);
            return dream is null ? Results.NotFound() : Results.Ok(dream);
        })
            .WithName("GetDream")
            .WithSummary("Returns one dream for the current user.");

        group.MapDelete("{id:guid}", async (
            Guid id,
            [FromServices] DeleteDreamHandler handler,
            CancellationToken cancellationToken) =>
        {
            var deleted = await handler.HandleAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
            .WithName("DeleteDream")
            .WithSummary("Deletes one dream for the current user.");

        return app;
    }
}
