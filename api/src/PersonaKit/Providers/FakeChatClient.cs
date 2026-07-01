using Microsoft.Extensions.AI;

namespace PersonaKit.Providers;

public sealed class FakeChatClient(string responseText = "{\"status\":\"ok\"}") : IChatClient
{
    private readonly List<FakeChatCall> _calls = [];

    public IReadOnlyList<FakeChatCall> Calls => _calls;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = messages.Select(message => message.Clone()).ToArray();
        _calls.Add(new FakeChatCall(snapshot, options));

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText))
        {
            ModelId = "fake",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("FakeChatClient only supports non-streaming responses.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

public sealed record FakeChatCall(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options);
