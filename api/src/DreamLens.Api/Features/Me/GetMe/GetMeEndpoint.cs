namespace DreamLens.Api.Features.Me.GetMe;

public static class GetMeEndpoint
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/me", (GetMeHandler handler) => Results.Ok(handler.Handle()))
            .RequireAuthorization()
            .WithName("GetMe")
            .WithTags("Identity")
            .WithSummary("Returns the authenticated user's identity snapshot.");

        return app;
    }
}
