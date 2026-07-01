using Microsoft.Extensions.AI;
using PersonaKit.Providers;
using PersonaKit.Providers.Usage;

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
}
