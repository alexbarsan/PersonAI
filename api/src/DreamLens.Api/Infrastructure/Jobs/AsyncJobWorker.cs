using System.Text.Json;
using System.Diagnostics;
using Amazon.SQS;
using Amazon.SQS.Model;
using DreamLens.Api.Infrastructure.Observability;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class AsyncJobWorker(
    IAmazonSQS sqs,
    IOptions<AsyncJobOptions> queueOptions,
    IOptions<AsyncJobWorkerOptions> workerOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<AsyncJobWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = queueOptions.Value.QueueUrl;
        if (!workerOptions.Value.Enabled || string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogInformation("Async job worker is disabled because no enabled queue is configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                WaitTimeSeconds = Math.Clamp(workerOptions.Value.PollWaitSeconds, 1, 20),
                VisibilityTimeout = Math.Clamp(queueOptions.Value.VisibilityTimeoutSeconds, 30, 43200),
                MaxNumberOfMessages = 1
            }, stoppingToken);

            foreach (var message in response.Messages ?? Enumerable.Empty<Message>())
            {
                await ProcessMessageAsync(queueUrl, message, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string queueUrl, Message message, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        AsyncJobMessage? jobMessage;
        try
        {
            jobMessage = JsonSerializer.Deserialize<AsyncJobMessage>(message.Body, JsonOptions);
            if (jobMessage is null)
            {
                throw new InvalidOperationException("Queue message is empty.");
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Deleting malformed async job message {MessageId}.", message.MessageId);
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            RecordDuration("malformed", "unknown", started);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
        var job = await ClaimAsync(db, jobMessage.JobId, cancellationToken);
        if (job is null)
        {
            var availableAt = await db.AsyncJobs
                .Where(candidate => candidate.Id == jobMessage.JobId
                    && candidate.Status == AsyncJobStatuses.Pending
                    && candidate.AvailableAt > DateTimeOffset.UtcNow)
                .Select(candidate => (DateTimeOffset?)candidate.AvailableAt)
                .SingleOrDefaultAsync(cancellationToken);

            if (availableAt is not null)
            {
                await DelayMessageAsync(queueUrl, message, availableAt.Value - DateTimeOffset.UtcNow, cancellationToken);
                RecordDuration("scheduled", jobMessage.JobType, started);
                return;
            }

            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            DreamLensMeters.AsyncJobsLeaseSkipped.Add(1, JobTypeTag(jobMessage.JobType));
            RecordDuration("lease_skipped", jobMessage.JobType, started);
            return;
        }

        try
        {
            var handler = scope.ServiceProvider.GetRequiredService<IAsyncJobHandler>();
            await handler.HandleAsync(jobMessage, cancellationToken);
            job.Status = AsyncJobStatuses.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            DreamLensMeters.AsyncJobsCompleted.Add(1, JobTypeTag(job.JobType));
            RecordDuration("completed", job.JobType, started);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var retryable = job.AttemptCount < Math.Max(1, workerOptions.Value.MaxAttempts);
            var retryDelay = GetRetryDelay(job.AttemptCount);
            job.Status = retryable
                ? AsyncJobStatuses.Pending
                : AsyncJobStatuses.Failed;
            job.AvailableAt = DateTimeOffset.UtcNow.Add(retryDelay);
            job.LockedUntil = null;
            job.LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogError(exception, "Async job {JobId} failed on attempt {Attempt}.", job.Id, job.AttemptCount);
            if (!retryable)
            {
                DreamLensMeters.AsyncJobsFailed.Add(1, JobTypeTag(job.JobType));
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                RecordDuration("failed", job.JobType, started);
            }
            else
            {
                DreamLensMeters.AsyncJobsRetried.Add(1, JobTypeTag(job.JobType));
                await DelayMessageAsync(queueUrl, message, retryDelay, cancellationToken);
                RecordDuration("retry", job.JobType, started);
            }
        }
    }

    private static async Task<AsyncJobRecord?> ClaimAsync(
        DreamLensDbContext db,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = await db.AsyncJobs
            .Where(job => job.Id == jobId
                && job.AvailableAt <= now
                && (job.Status == AsyncJobStatuses.Pending
                    || (job.Status == AsyncJobStatuses.Processing && job.LockedUntil < now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, AsyncJobStatuses.Processing)
                .SetProperty(job => job.AttemptCount, job => job.AttemptCount + 1)
                .SetProperty(job => job.LockedUntil, now.AddSeconds(300))
                .SetProperty(job => job.UpdatedAt, now), cancellationToken);

        return updated == 0
            ? null
            : await db.AsyncJobs.SingleAsync(job => job.Id == jobId, cancellationToken);
    }

    private static KeyValuePair<string, object?> JobTypeTag(string jobType) => new("job.type", jobType);

    private TimeSpan GetRetryDelay(int attemptCount)
    {
        var baseSeconds = Math.Clamp(workerOptions.Value.RetryBaseDelaySeconds, 1, 3600);
        var maximumSeconds = Math.Clamp(workerOptions.Value.RetryMaxDelaySeconds, baseSeconds, 43200);
        var multiplier = Math.Pow(2, Math.Min(16, Math.Max(0, attemptCount - 1)));
        return TimeSpan.FromSeconds(Math.Min(maximumSeconds, baseSeconds * multiplier));
    }

    private async Task DelayMessageAsync(
        string queueUrl,
        Message message,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        var visibilityTimeout = (int)Math.Clamp(Math.Ceiling(delay.TotalSeconds), 1, 43200);
        await sqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle,
            VisibilityTimeout = visibilityTimeout
        }, cancellationToken);
    }

    private static void RecordDuration(string outcome, string jobType, long started)
    {
        DreamLensMeters.AsyncJobProcessingDuration.Record(
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            new KeyValuePair<string, object?>("job.outcome", outcome),
            JobTypeTag(jobType));
    }
}
