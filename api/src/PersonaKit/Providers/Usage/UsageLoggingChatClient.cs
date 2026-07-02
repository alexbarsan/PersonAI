using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace PersonaKit.Providers.Usage;

public sealed class UsageLoggingChatClient(
    IChatClient inner,
    IChatUsageSink sink,
    UsageCostOptions costOptions) : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await inner.GetResponseAsync(messages, options, cancellationToken);
        var latency = Stopwatch.GetElapsedTime(started);
        var usage = response.Usage;

        var model = response.ModelId ?? options?.ModelId ?? "unknown";
        var estimatedCost = EstimateCost(usage);
        await sink.RecordAsync(
            new ChatUsageRecord(
                model,
                latency,
                usage?.InputTokenCount,
                usage?.OutputTokenCount,
                usage?.TotalTokenCount,
                estimatedCost),
            cancellationToken);
        PersonaKitMeters.AiEstimatedCostUsd.Record(
            decimal.ToDouble(estimatedCost),
            new KeyValuePair<string, object?>("model", model));
        PersonaKitMeters.AiTokens.Record(
            usage?.TotalTokenCount ?? 0,
            new KeyValuePair<string, object?>("model", model));

        return response;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("UsageLoggingChatClient only supports non-streaming responses in S5.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(UsageLoggingChatClient) ? this : inner.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        inner.Dispose();
    }

    private decimal EstimateCost(UsageDetails? usage)
    {
        if (usage is null)
        {
            return 0;
        }

        var input = (usage.InputTokenCount ?? 0) * costOptions.InputCostPerMillionTokens / 1_000_000m;
        var output = (usage.OutputTokenCount ?? 0) * costOptions.OutputCostPerMillionTokens / 1_000_000m;
        return input + output;
    }
}
