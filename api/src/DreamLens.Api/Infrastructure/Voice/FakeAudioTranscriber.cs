namespace DreamLens.Api.Infrastructure.Voice;

public sealed class FakeAudioTranscriber : IAudioTranscriber
{
    public Task<AudioTranscriptionResult> TranscribeAsync(AudioTranscriptionRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AudioTranscriptionResult(
            "Voice capture transcription is available in the configured environment.",
            "Fake",
            "fake-transcriber-v1"));
    }
}
