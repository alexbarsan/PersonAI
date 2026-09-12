using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed record OperationsQueueSnapshot(
    string Status,
    int Available,
    int InFlight,
    int Delayed,
    int DeadLetter,
    string? Error);

public interface IOperationsQueueMonitor
{
    Task<OperationsQueueSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class SqsOperationsQueueMonitor(
    IAmazonSQS sqs,
    IOptions<AsyncJobOptions> options,
    ILogger<SqsOperationsQueueMonitor> logger) : IOperationsQueueMonitor
{
    public async Task<OperationsQueueSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.QueueUrl))
        {
            return new OperationsQueueSnapshot("unavailable", 0, 0, 0, 0, "Queue URL is not configured.");
        }

        try
        {
            var response = await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = options.Value.QueueUrl,
                AttributeNames =
                [
                    QueueAttributeName.ApproximateNumberOfMessages,
                    QueueAttributeName.ApproximateNumberOfMessagesNotVisible,
                    QueueAttributeName.ApproximateNumberOfMessagesDelayed,
                    QueueAttributeName.RedrivePolicy
                ]
            }, cancellationToken);
            var deadLetterCount = await GetDeadLetterCountAsync(response.Attributes, cancellationToken);
            return new OperationsQueueSnapshot(
                "available",
                ReadCount(response.Attributes, QueueAttributeName.ApproximateNumberOfMessages),
                ReadCount(response.Attributes, QueueAttributeName.ApproximateNumberOfMessagesNotVisible),
                ReadCount(response.Attributes, QueueAttributeName.ApproximateNumberOfMessagesDelayed),
                deadLetterCount,
                null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Operations queue snapshot could not be loaded.");
            return new OperationsQueueSnapshot("unavailable", 0, 0, 0, 0, "Queue telemetry is temporarily unavailable.");
        }
    }

    private async Task<int> GetDeadLetterCountAsync(
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        if (!attributes.TryGetValue(QueueAttributeName.RedrivePolicy, out var policy))
        {
            return 0;
        }

        using var document = JsonDocument.Parse(policy);
        var arn = document.RootElement.GetProperty("deadLetterTargetArn").GetString();
        var queueName = arn?.Split(':').LastOrDefault();
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return 0;
        }

        var separator = options.Value.QueueUrl.LastIndexOf('/');
        if (separator < 0)
        {
            return 0;
        }
        var queueUrl = $"{options.Value.QueueUrl[..(separator + 1)]}{queueName}";
        var response = await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = [QueueAttributeName.ApproximateNumberOfMessages]
        }, cancellationToken);
        return ReadCount(response.Attributes, QueueAttributeName.ApproximateNumberOfMessages);
    }

    private static int ReadCount(IReadOnlyDictionary<string, string> attributes, string name) =>
        attributes.TryGetValue(name, out var value) && int.TryParse(value, out var count) ? count : 0;
}
