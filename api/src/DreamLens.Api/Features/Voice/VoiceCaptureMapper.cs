using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Voice;

internal static class VoiceCaptureMapper
{
    public static VoiceCaptureResponse Map(VoiceCaptureRecord capture, Guid? jobId, string? recordingUrl) => new(
        capture.Id,
        capture.Status,
        capture.DurationSeconds,
        capture.RetainRecording,
        capture.Transcript,
        recordingUrl,
        jobId,
        capture.ErrorMessage,
        capture.CreatedAt);
}
