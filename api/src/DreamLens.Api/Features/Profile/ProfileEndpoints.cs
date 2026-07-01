using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Profile;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/profile")
            .RequireAuthorization()
            .WithTags("Profile");

        group.MapGet("", async ([FromServices] GetProfileHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("GetProfile")
            .WithSummary("Returns the current user's profile.");

        group.MapPut("", async (
            UpdateProfileRequest request,
            [FromServices] UpdateProfileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            return result.IsValid
                ? Results.Ok(result.Profile)
                : Results.BadRequest(result.Errors);
        })
            .WithName("UpdateProfile")
            .WithSummary("Updates the current user's profile and consent flags.");

        return app;
    }
}
