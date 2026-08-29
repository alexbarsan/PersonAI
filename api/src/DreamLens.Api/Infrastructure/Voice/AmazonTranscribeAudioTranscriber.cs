using System.Text.Json;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using DreamLens.Api.Infrastructure.Assets;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Voice;

public sealed class AmazonTranscribeAudioTranscriber(
    IAmazonTranscribeService transcribe,
    IPrivateAssetStore assetStore,
    IOptions<VoiceTranscriptionOptions> options,
    IOptions<PrivateAssetOptions> assetOptions) : IAudioTranscriber
{
    public async Task<AudioTranscriptionResult> TranscribeAsync(AudioTranscriptionRequest request, CancellationToken cancellationToken)
    {
        var jobName = $"dreamlens-voice-{request.CaptureId:N}";
        var outputKey = $"voice-transcripts/{request.CaptureId:N}.json";
        var job = await GetOrStartJobAsync(jobName, outputKey, request, cancellationToken);
        var maxWait = TimeSpan.FromSeconds(Math.Clamp(options.Value.MaxWaitSeconds, 15, 900));
        var deadline = DateTimeOffset.UtcNow.Add(maxWait);

        while (string.Equals(job.TranscriptionJobStatus, TranscriptionJobStatus.IN_PROGRESS, StringComparison.OrdinalIgnoreCase)
            || string.Equals(job.TranscriptionJobStatus, TranscriptionJobStatus.QUEUED, StringComparison.OrdinalIgnoreCase))
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("Amazon Transcribe did not finish before the worker deadline.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 2, 20)), cancellationToken);
            job = (await transcribe.GetTranscriptionJobAsync(new GetTranscriptionJobRequest
            {
                TranscriptionJobName = jobName
            }, cancellationToken)).TranscriptionJob;
        }

        if (!string.Equals(job.TranscriptionJobStatus, TranscriptionJobStatus.COMPLETED, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(job.FailureReason ?? "Amazon Transcribe did not complete the job.");
        }

        try
        {
            await using var transcriptStream = await assetStore.OpenReadAsync(outputKey, cancellationToken);
            using var document = await JsonDocument.ParseAsync(transcriptStream, cancellationToken: cancellationToken);
            var transcript = document.RootElement
                .GetProperty("results")
                .GetProperty("transcripts")[0]
                .GetProperty("transcript")
                .GetString();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new InvalidOperationException("Amazon Transcribe returned an empty transcript.");
            }

            return new AudioTranscriptionResult(transcript, "Amazon Transcribe", options.Value.Model);
        }
        finally
        {
            await assetStore.DeleteAsync(outputKey, cancellationToken);
        }
    }

    private async Task<TranscriptionJob> GetOrStartJobAsync(
        string jobName,
        string outputKey,
        AudioTranscriptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await transcribe.GetTranscriptionJobAsync(new GetTranscriptionJobRequest
            {
                TranscriptionJobName = jobName
            }, cancellationToken)).TranscriptionJob;
        }
        catch (BadRequestException)
        {
            var start = new StartTranscriptionJobRequest
            {
                TranscriptionJobName = jobName,
                Media = new Media
                {
                    MediaFileUri = $"s3://{assetOptions.Value.BucketName}/{request.SourceAssetKey}"
                },
                MediaFormat = ToMediaFormat(request.ContentType),
                OutputBucketName = assetOptions.Value.BucketName,
                OutputKey = outputKey
            };
            if (string.IsNullOrWhiteSpace(request.Language))
            {
                start.IdentifyLanguage = true;
            }
            else
            {
                start.LanguageCode = request.Language;
            }

            return (await transcribe.StartTranscriptionJobAsync(start, cancellationToken)).TranscriptionJob;
        }
    }

    private static string ToMediaFormat(string contentType) => contentType.ToLowerInvariant() switch
    {
        "audio/mpeg" => "mp3",
        "audio/wav" or "audio/x-wav" => "wav",
        "audio/ogg" => "ogg",
        "audio/webm" => "webm",
        "audio/m4a" => "m4a",
        "audio/mp4" => "mp4",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), "The audio content type is not supported by Amazon Transcribe.")
    };
}
