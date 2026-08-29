using System.Diagnostics;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Voice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class VoiceTranscriptionJobHandler(
    DreamLensDbContext dbContext,
    IAudioTranscriber transcriber,
    IPrivateAssetStore assetStore,
    IOptions<VoiceTranscriptionOptions> options) : IAsyncJobHandler
{
    public string JobType => AsyncJobTypes.VoiceTranscription;

    public async Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<VoiceTranscriptionJobPayload>(message.PayloadJson)
            ?? throw new InvalidOperationException("Voice transcription job payload is invalid.");
        var capture = await dbContext.VoiceCaptures.SingleOrDefaultAsync(
            candidate => candidate.Id == payload.CaptureId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Voice capture was not found.");
        if (capture.Status == VoiceCaptureStatuses.Completed)
        {
            return;
        }

        var started = Stopwatch.GetTimestamp();
        capture.Status = VoiceCaptureStatuses.Transcribing;
        capture.ErrorMessage = null;
        capture.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await transcriber.TranscribeAsync(
                new AudioTranscriptionRequest(capture.Id, capture.SourceAssetKey, capture.ContentType, capture.Language, capture.DurationSeconds),
                cancellationToken);
            capture.Status = VoiceCaptureStatuses.Completed;
            capture.Transcript = result.Transcript[..Math.Min(result.Transcript.Length, 8000)];
            capture.UpdatedAt = DateTimeOffset.UtcNow;
            if (!capture.RetainRecording)
            {
                await assetStore.DeleteAsync(capture.SourceAssetKey, cancellationToken);
            }

            dbContext.AiCostLedger.Add(CreateLedger(message, result.Provider, result.Model, "completed", null, capture.DurationSeconds, Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            capture.Status = VoiceCaptureStatuses.Failed;
            capture.ErrorMessage = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            capture.UpdatedAt = DateTimeOffset.UtcNow;
            if (!capture.RetainRecording)
            {
                await assetStore.DeleteAsync(capture.SourceAssetKey, cancellationToken);
            }

            dbContext.AiCostLedger.Add(CreateLedger(message, "Amazon Transcribe", options.Value.Model, "failed", exception.GetType().Name, capture.DurationSeconds, Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private AiCostLedgerRecord CreateLedger(
        AsyncJobMessage message,
        string provider,
        string model,
        string status,
        string? failureKind,
        int durationSeconds,
        TimeSpan latency) => new()
        {
            UserSubject = message.UserSubject,
            Provider = provider,
            Model = model,
            PersonaId = "voice-capture",
            OperationType = "voice.transcription",
            Status = status,
            FailureKind = failureKind,
            AttemptCount = 1,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = Math.Max(0, durationSeconds) * options.Value.EstimatedCostPerSecondUsd
        };

    public sealed record VoiceTranscriptionJobPayload(Guid CaptureId);
}
