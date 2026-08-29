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
                return Results.Json(result.Errors, statusCode: result.ErrorStatusCode);
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

        group.MapGet("{id:guid}/facts", async (
            Guid id,
            [FromServices] GetDreamFactsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var facts = await handler.HandleAsync(id, cancellationToken);
            return facts is null ? Results.NotFound() : Results.Ok(facts);
        })
            .WithName("GetDreamFacts")
            .WithSummary("Returns normalized extracted facts for one dream owned by the current user.");

        group.MapGet("{id:guid}/similar", async (
            Guid id,
            [FromQuery] int? limit,
            [FromServices] GetSimilarDreamsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var similarDreams = await handler.HandleAsync(id, limit ?? 5, cancellationToken);
            return similarDreams is null ? Results.NotFound() : Results.Ok(similarDreams);
        })
            .WithName("GetSimilarDreams")
            .WithSummary("Returns the current user's closest semantic dream matches when embeddings are available.");

        group.MapPost("{id:guid}/image", async (
            Guid id,
            RequestDreamImageRequest request,
            [FromServices] RequestDreamImageHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, request, cancellationToken);
            return result.Image is not null
                ? Results.Accepted($"/v1/dreams/{id}/image", result.Image)
                : Results.Json(result.Errors, statusCode: result.StatusCode);
        })
            .WithName("RequestDreamImage")
            .WithSummary("Queues a premium dream image for asynchronous generation.");

        group.MapGet("{id:guid}/image", async (
            Guid id,
            [FromServices] GetDreamImageHandler handler,
            CancellationToken cancellationToken) =>
        {
            var image = await handler.HandleAsync(id, cancellationToken);
            return image is null ? Results.NotFound() : Results.Ok(image);
        })
            .WithName("GetDreamImage")
            .WithSummary("Returns the latest generated image for a dream owned by the current user.");

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
