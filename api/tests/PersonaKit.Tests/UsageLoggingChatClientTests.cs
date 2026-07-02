using Microsoft.Extensions.AI;
using PersonaKit.Providers;
using PersonaKit.Providers.Usage;
using System.Diagnostics.Metrics;

namespace PersonaKit.Tests;

public sealed class UsageLoggingChatClientTests
{
    [Fact]
    public async Task RecordsUsageWithoutPromptText()
    {
        var sink = new InMemoryChatUsageSink();
        var inner = new StaticChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer"))
        {
            ModelId = "deepseek-chat",
            Usage = new UsageDetails
            {
                InputTokenCount = 10,
                OutputTokenCount = 5,
                TotalTokenCount = 15
            }
        });
        var client = new UsageLoggingChatClient(
            inner,
            sink,
            new UsageCostOptions { InputCostPerMillionTokens = 0.14m, OutputCostPerMillionTokens = 0.28m });

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "secret prompt text")]);

        var record = sink.Records.Single();
        Assert.Equal("deepseek-chat", record.Model);
        Assert.Equal(10, record.InputTokens);
        Assert.Equal(5, record.OutputTokens);
        Assert.Equal(15, record.TotalTokens);
        Assert.True(record.Latency > TimeSpan.Zero);
        Assert.Equal(0.0000028m, record.EstimatedCostUsd);
        Assert.DoesNotContain("secret", record.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", record.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmitsAiCostMetricsWithoutPromptText()
    {
        var measurements = new List<(string Name, double Value, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "PersonaKit")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            measurements.Add((instrument.Name, value, ToDictionary(tags)));
        });
        listener.Start();

        var inner = new StaticChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer"))
        {
            ModelId = "deepseek-chat",
            Usage = new UsageDetails
            {
                InputTokenCount = 10,
                OutputTokenCount = 5,
                TotalTokenCount = 15
            }
        });
        var client = new UsageLoggingChatClient(
            inner,
            new InMemoryChatUsageSink(),
            new UsageCostOptions { InputCostPerMillionTokens = 0.14m, OutputCostPerMillionTokens = 0.28m });

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "secret prompt text")]);
        listener.RecordObservableInstruments();

        var cost = Assert.Single(measurements, measurement => measurement.Name == "personakit.ai.estimated_cost_usd");
        var tokens = Assert.Single(measurements, measurement => measurement.Name == "personakit.ai.tokens");
        Assert.Equal(0.0000028d, cost.Value, precision: 12);
        Assert.Equal(15d, tokens.Value);
        Assert.Equal("deepseek-chat", cost.Tags["model"]);
        Assert.DoesNotContain(cost.Tags.Values, value => value?.ToString()?.Contains("secret", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(tokens.Tags.Values, value => value?.ToString()?.Contains("prompt", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static Dictionary<string, object?> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            values[tag.Key] = tag.Value;
        }

        return values;
    }
}
