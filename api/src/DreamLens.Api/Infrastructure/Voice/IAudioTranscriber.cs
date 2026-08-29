namespace DreamLens.Api.Infrastructure.Voice;

public interface IAudioTranscriber
{
    Task<AudioTranscriptionResult> TranscribeAsync(AudioTranscriptionRequest request, CancellationToken cancellationToken);
}

public sealed record AudioTranscriptionRequest(
    Guid CaptureId,
    string SourceAssetKey,
    string ContentType,
    string? Language,
    int DurationSeconds);

public sealed record AudioTranscriptionResult(string Transcript, string Provider, string Model);
