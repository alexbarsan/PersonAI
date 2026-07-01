using System.Net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;
using PersonaKit.Providers;
using PersonaKit.Providers.Resilience;

namespace PersonaKit.Tests;

public sealed class ResilienceChatClientTests
{
    [Fact]
    public async Task RetryPolicyRetries429And5xxTwice()
    {
        var inner = new QueueChatClient(
            new ChatProviderException(HttpStatusCode.TooManyRequests, "rate limited"),
            new ChatProviderException(HttpStatusCode.InternalServerError, "server failed"),
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var options = new ChatResilienceOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            MaxRetryAttempts = 2,
            RetryBaseDelay = TimeSpan.Zero,
            CircuitBreakerFailureRatio = 1,
            CircuitBreakerMinimumThroughput = 10,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30)
        };
        var client = new ResilienceChatClient(inner, options);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

        Assert.Equal("ok", response.Text);
        Assert.Equal(3, inner.CallCount);
    }

    [Fact]
    public async Task CircuitBreakerOpensAfterConfiguredFailures()
    {
        var timeProvider = new FakeTimeProvider();
        var inner = new AlwaysFailingChatClient(HttpStatusCode.InternalServerError);
        var options = new ChatResilienceOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            MaxRetryAttempts = 0,
            RetryBaseDelay = TimeSpan.Zero,
            CircuitBreakerFailureRatio = 1,
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1)
        };
        var client = new ResilienceChatClient(inner, options, timeProvider);

        await Assert.ThrowsAsync<ChatProviderException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "first")]));
        await Assert.ThrowsAsync<ChatProviderException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "second")]));
        await Assert.ThrowsAsync<ChatCircuitOpenException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "third")]));

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task TimeoutCancelsSlowInnerClient()
    {
        var inner = new DelayedChatClient(TimeSpan.FromSeconds(30));
        var client = new ResilienceChatClient(inner, new ChatResilienceOptions
        {
            Timeout = TimeSpan.FromMilliseconds(50),
            MaxRetryAttempts = 0,
            RetryBaseDelay = TimeSpan.Zero,
            CircuitBreakerFailureRatio = 1,
            CircuitBreakerMinimumThroughput = 10,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30)
        });

        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "slow")]));
    }
}
