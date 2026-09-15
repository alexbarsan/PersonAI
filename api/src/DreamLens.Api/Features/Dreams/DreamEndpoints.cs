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
            [FromQuery] string? query,
            [FromQuery] string? mood,
            [FromQuery] string? tag,
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] ListDreamsHandler handler,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new DreamJournalQuery(query, mood, tag, from, to, page, pageSize), cancellationToken)))
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
                : result.ErrorStatusCode == StatusCodes.Status503ServiceUnavailable
                    ? Results.Json(result.Dream, statusCode: result.ErrorStatusCode)
                : Results.Accepted($"/v1/dreams/{result.Dream!.Id}", result.Dream);
        })
            .WithName("SubmitDream")
            .WithSummary("Submits a dream and returns an interpretation result.");

        group.MapPost("{id:guid}/retry", async (
            Guid id,
            [FromServices] DreamInterpretationLifecycleHandler handler,
            CancellationToken cancellationToken) =>
        {
            var dream = await handler.RetryAsync(id, cancellationToken);
            return dream is null ? Results.NotFound() : Results.Accepted($"/v1/dreams/{id}", dream);
        })
            .WithName("RetryDreamInterpretation")
            .WithSummary("Retries a failed interpretation for an owned dream.");

        group.MapPost("{id:guid}/cancel", async (
            Guid id,
            [FromServices] DreamInterpretationLifecycleHandler handler,
            CancellationToken cancellationToken) =>
        {
            var dream = await handler.CancelAsync(id, cancellationToken);
            return dream is null ? Results.NotFound() : Results.Ok(dream);
        })
            .WithName("CancelDreamInterpretation")
            .WithSummary("Cancels a queued or processing interpretation for an owned dream.");

        group.MapPost("ask", async (
            AskDreamsRequest request,
            [FromServices] AskDreamsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return result.Response is not null
                ? Results.Ok(result.Response)
                : Results.Json(result.Errors, statusCode: result.StatusCode);
        })
            .WithName("AskDreamHistory")
            .WithSummary("Answers a reflective question using owner-scoped semantic dream memory.");

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

        group.MapGet("{id:guid}/deep-interpretation", async (
            Guid id,
            [FromServices] DeepInterpretationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var interpretation = await handler.GetAsync(id, cancellationToken);
            return interpretation is null ? Results.NotFound() : Results.Ok(interpretation);
        })
            .WithName("GetDeepInterpretation")
            .WithSummary("Returns the persisted Premium Cognitive Analysis for an owned dream.");

        group.MapPost("{id:guid}/deep-interpretation", async (
            Guid id,
            [FromServices] DeepInterpretationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.CreateAsync(id, cancellationToken);
            return result.Interpretation is not null
                ? Results.Ok(result.Interpretation)
                : Results.Json(result.Errors, statusCode: result.StatusCode);
        })
            .WithName("CreateDeepInterpretation")
            .WithSummary("Creates one persisted, Premium-only Cognitive Analysis using related dream context.");

        group.MapGet("{id:guid}/feedback", async (
            Guid id,
            [FromServices] GetDreamFeedbackHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.DreamExists ? Results.Ok(result.Feedback) : Results.NotFound();
        })
            .WithName("GetDreamFeedback")
            .WithSummary("Returns the current user's saved interpretation feedback for one dream.");

        group.MapPut("{id:guid}/feedback", async (
            Guid id,
            UpdateDreamFeedbackRequest request,
            [FromServices] UpdateDreamFeedbackHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, request, cancellationToken);
            return result.Feedback is not null
                ? Results.Ok(result.Feedback)
                : Results.Json(result.Errors, statusCode: result.StatusCode);
        })
            .WithName("UpdateDreamFeedback")
            .WithSummary("Creates or replaces like/dislike feedback for an owned dream interpretation.");

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

        group.MapGet("{id:guid}/image/wait", async (
            Guid id,
            DateTimeOffset? after,
            int? timeoutSeconds,
            [FromServices] GetDreamImageHandler handler,
            CancellationToken cancellationToken) =>
        {
            var image = await handler.WaitForChangeAsync(id, after, timeoutSeconds ?? 20, cancellationToken);
            return image is null ? Results.NotFound() : Results.Ok(image);
        })
            .WithName("WaitForDreamImage")
            .WithSummary("Waits for the latest owned dream image status to change, with a bounded timeout.");

        return app;
    }
}
