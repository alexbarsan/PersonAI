using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
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

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(queueUrl, message, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string queueUrl, Message message, CancellationToken cancellationToken)
    {
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
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
        var job = await ClaimAsync(db, jobMessage.JobId, cancellationToken);
        if (job is null)
        {
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
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
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Status = job.AttemptCount >= Math.Max(1, workerOptions.Value.MaxAttempts)
                ? AsyncJobStatuses.Failed
                : AsyncJobStatuses.Pending;
            job.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, job.AttemptCount)));
            job.LockedUntil = null;
            job.LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogError(exception, "Async job {JobId} failed on attempt {Attempt}.", job.Id, job.AttemptCount);
            if (job.Status == AsyncJobStatuses.Failed)
            {
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
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
}
