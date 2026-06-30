namespace DreamLens.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
            .WithTags("Health");

        group.MapGet("/live", () => Results.Ok(new HealthResponse("Healthy")))
            .WithName("GetLiveHealth")
            .WithSummary("Reports whether the API process is alive.");

        group.MapGet("/ready", async (IDatabaseReadinessProbe readinessProbe, CancellationToken cancellationToken) =>
            await readinessProbe.IsReadyAsync(cancellationToken)
                ? Results.Ok(new HealthResponse("Ready"))
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
            .WithName("GetReadyHealth")
            .WithSummary("Reports whether the API is ready to receive traffic.");

        return app;
    }

    private sealed record HealthResponse(string Status);
}
