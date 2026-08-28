using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Jobs;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/jobs/{id:guid}", async (
            Guid id,
            [FromServices] GetJobHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
            .RequireAuthorization()
            .WithTags("Jobs")
            .WithName("GetJob")
            .WithSummary("Returns the status of an asynchronous job owned by the current user.");

        return app;
    }
}
