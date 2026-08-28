using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class SqsAsyncJobQueue(
    IAmazonSQS sqs,
    IOptions<AsyncJobOptions> options) : IAsyncJobQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var queueUrl = options.Value.QueueUrl;
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new InvalidOperationException("Jobs:QueueUrl must be configured before publishing asynchronous jobs.");
        }

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message, JsonOptions)
        }, cancellationToken);
    }
}
