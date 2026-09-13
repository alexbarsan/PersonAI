using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Content;

public static class DailyDreamContentEndpoints
{
    public static IEndpointRouteBuilder MapDailyDreamContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/dream-content", async (
            [FromQuery] DateOnly? date,
            [FromServices] GetDailyDreamContentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (selectedDate < DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1)
                || selectedDate > DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2))
            {
                return Results.BadRequest(new { date = new[] { "Date must be within the supported content range." } });
            }

            return Results.Ok(await handler.HandleAsync(selectedDate, cancellationToken));
        })
            .WithName("GetDailyDreamContent")
            .WithTags("Dream content")
            .WithSummary("Returns the database-backed quote and dream facts for a calendar date.")
            .AllowAnonymous();

        return app;
    }
}
