using Microsoft.AspNetCore.Mvc;

namespace DreamLens.Api.Features.Voice;

public static class VoiceEndpoints
{
    public static IEndpointRouteBuilder MapVoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/voice-captures")
            .RequireAuthorization()
            .WithTags("Voice captures");

        group.MapPost("", async (
            [FromForm] IFormFile? audio,
            [FromForm] int durationSeconds,
            [FromForm] bool retainRecording,
            [FromForm] string? language,
            [FromServices] UploadVoiceCaptureHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(audio, durationSeconds, retainRecording, language, cancellationToken);
            return result.Capture is not null
                ? Results.Accepted($"/v1/voice-captures/{result.Capture.Id}", result.Capture)
                : Results.Json(result.Errors, statusCode: result.StatusCode);
        })
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .WithName("UploadVoiceCapture")
            .WithSummary("Uploads a premium voice capture for transcription. Recordings are deleted after transcription unless retained explicitly.");

        group.MapGet("{id:guid}", async (
            Guid id,
            [FromServices] GetVoiceCaptureHandler handler,
            CancellationToken cancellationToken) =>
        {
            var capture = await handler.HandleAsync(id, cancellationToken);
            return capture is null ? Results.NotFound() : Results.Ok(capture);
        })
            .WithName("GetVoiceCapture")
            .WithSummary("Returns the current user's voice-capture transcript and processing status.");

        return app;
    }
}
