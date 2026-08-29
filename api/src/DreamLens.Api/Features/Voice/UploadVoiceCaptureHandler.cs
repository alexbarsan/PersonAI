using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Voice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Voice;

public sealed class UploadVoiceCaptureHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    IPrivateAssetStore assetStore,
    IOptions<VoiceTranscriptionOptions> options,
    AsyncJobService? asyncJobService = null)
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg", "audio/wav", "audio/x-wav", "audio/ogg", "audio/webm", "audio/m4a", "audio/mp4"
    };

    public async Task<UploadVoiceCaptureResult> HandleAsync(
        IFormFile? audio,
        int durationSeconds,
        bool retainRecording,
        string? language,
        CancellationToken cancellationToken)
    {
        var voiceOptions = options.Value;
        if (!voiceOptions.Enabled || asyncJobService is null)
        {
            return UploadVoiceCaptureResult.Unavailable();
        }

        if (!entitlementService.GetEntitlement(currentUser.Subject).DeepAnalysisEnabled)
        {
            return UploadVoiceCaptureResult.NotEntitled();
        }

        if (audio is null || audio.Length == 0)
        {
            return UploadVoiceCaptureResult.Invalid("audio", "An audio recording is required.");
        }

        if (audio.Length > voiceOptions.MaxUploadBytes)
        {
            return UploadVoiceCaptureResult.Invalid("audio", $"The recording must be {voiceOptions.MaxUploadBytes / 1024 / 1024} MB or smaller.");
        }

        if (!SupportedContentTypes.Contains(audio.ContentType))
        {
            return UploadVoiceCaptureResult.Invalid("audio", "The recording format is not supported.");
        }

        if (durationSeconds is < 1 || durationSeconds > voiceOptions.MaxDurationSeconds)
        {
            return UploadVoiceCaptureResult.Invalid("durationSeconds", $"Recordings must be between 1 and {voiceOptions.MaxDurationSeconds} seconds.");
        }

        var normalizedLanguage = NormalizeLanguage(language);
        if (normalizedLanguage is null && !string.IsNullOrWhiteSpace(language))
        {
            return UploadVoiceCaptureResult.Invalid("language", "The requested language code is invalid.");
        }

        var since = DateTimeOffset.UtcNow.Date;
        var usedToday = await dbContext.VoiceCaptures.CountAsync(
            capture => capture.UserSubject == currentUser.Subject && capture.CreatedAt >= since,
            cancellationToken);
        if (usedToday >= voiceOptions.DailyLimit)
        {
            return UploadVoiceCaptureResult.QuotaExceeded();
        }

        var capture = new VoiceCaptureRecord
        {
            UserSubject = currentUser.Subject,
            ContentType = audio.ContentType.ToLowerInvariant(),
            DurationSeconds = durationSeconds,
            RetainRecording = retainRecording,
            Language = normalizedLanguage,
            SourceAssetKey = $"voice-input/{Guid.NewGuid():N}{ExtensionFor(audio.ContentType)}"
        };
        dbContext.VoiceCaptures.Add(capture);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await using var audioStream = audio.OpenReadStream();
            await assetStore.PutAsync(capture.SourceAssetKey, audioStream, capture.ContentType, cancellationToken);
            var job = await asyncJobService.EnqueueAsync(
                $"{AsyncJobTypes.VoiceTranscription}:{capture.Id:N}",
                AsyncJobTypes.VoiceTranscription,
                currentUser.Subject,
                capture.Id,
                new VoiceTranscriptionJobHandler.VoiceTranscriptionJobPayload(capture.Id),
                cancellationToken);
            return UploadVoiceCaptureResult.Accepted(VoiceCaptureMapper.Map(capture, job.Id, null));
        }
        catch (Exception exception)
        {
            capture.Status = VoiceCaptureStatuses.Failed;
            capture.ErrorMessage = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            capture.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return UploadVoiceCaptureResult.Failed();
        }
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var trimmed = language.Trim();
        return trimmed.Length is >= 2 and <= 32 && trimmed.All(character => char.IsLetter(character) || character == '-')
            ? trimmed
            : null;
    }

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "audio/mpeg" => ".mp3",
        "audio/wav" or "audio/x-wav" => ".wav",
        "audio/ogg" => ".ogg",
        "audio/webm" => ".webm",
        "audio/m4a" => ".m4a",
        "audio/mp4" => ".mp4",
        _ => ".audio"
    };
}

public sealed record UploadVoiceCaptureResult(int StatusCode, VoiceCaptureResponse? Capture, Dictionary<string, string[]>? Errors)
{
    public static UploadVoiceCaptureResult Accepted(VoiceCaptureResponse capture) => new(StatusCodes.Status202Accepted, capture, null);
    public static UploadVoiceCaptureResult Unavailable() => new(StatusCodes.Status503ServiceUnavailable, null, new Dictionary<string, string[]> { ["voiceTranscription"] = ["Voice transcription is not available yet."] });
    public static UploadVoiceCaptureResult NotEntitled() => new(StatusCodes.Status403Forbidden, null, new Dictionary<string, string[]> { ["entitlement"] = ["Voice transcription requires premium access."] });
    public static UploadVoiceCaptureResult QuotaExceeded() => new(StatusCodes.Status429TooManyRequests, null, new Dictionary<string, string[]> { ["quota"] = ["You have reached today's voice transcription limit."] });
    public static UploadVoiceCaptureResult Invalid(string field, string error) => new(StatusCodes.Status400BadRequest, null, new Dictionary<string, string[]> { [field] = [error] });
    public static UploadVoiceCaptureResult Failed() => new(StatusCodes.Status503ServiceUnavailable, null, new Dictionary<string, string[]> { ["voiceTranscription"] = ["The recording could not be queued. Please try again."] });
}
