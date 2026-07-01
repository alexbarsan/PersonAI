using Microsoft.Extensions.AI;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using PersonaKit.Providers;

namespace PersonaKit.Providers.Resilience;

public sealed class ResilienceChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly ResiliencePipeline<ChatResponse> _pipeline;

    public ResilienceChatClient(IChatClient inner, ChatResilienceOptions options)
        : this(inner, options, TimeProvider.System)
    {
    }

    public ResilienceChatClient(IChatClient inner, ChatResilienceOptions options, TimeProvider timeProvider)
    {
        _inner = inner;
        var builder = new ResiliencePipelineBuilder<ChatResponse>
        {
            TimeProvider = timeProvider
        }
            .AddTimeout(new TimeoutStrategyOptions { Timeout = options.Timeout });

        if (options.MaxRetryAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions<ChatResponse>
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = options.RetryBaseDelay,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<ChatResponse>().Handle<ChatProviderException>(exception => exception.IsTransient)
            });
        }

        _pipeline = builder
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<ChatResponse>
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    SamplingDuration = options.CircuitBreakerSamplingDuration,
                    BreakDuration = options.CircuitBreakerBreakDuration,
                    ShouldHandle = new PredicateBuilder<ChatResponse>().Handle<ChatProviderException>(exception => exception.IsTransient)
                })
            .Build();
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = messages.ToArray();

        try
        {
            return await _pipeline.ExecuteAsync(
                async token => await _inner.GetResponseAsync(snapshot, options, token),
                cancellationToken);
        }
        catch (BrokenCircuitException exception)
        {
            throw new ChatCircuitOpenException(exception);
        }
        catch (TimeoutRejectedException exception)
        {
            throw new TimeoutException("Chat provider request timed out.", exception);
        }
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("ResilienceChatClient only supports non-streaming responses in S5.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(ResilienceChatClient) ? this : _inner.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        _inner.Dispose();
    }
}
