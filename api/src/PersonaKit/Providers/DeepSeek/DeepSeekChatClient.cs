using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PersonaKit.Providers;

namespace PersonaKit.Providers.DeepSeek;

public sealed class DeepSeekChatClient(HttpClient httpClient, IOptions<DeepSeekOptions> options) : IChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly DeepSeekOptions _options = options.Value;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek:ApiKey must be configured.");
        }

        httpClient.BaseAddress ??= _options.BaseUrl;

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(CreateRequest(messages, options), options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ChatProviderException(response.StatusCode, $"DeepSeek request failed with {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<DeepSeekResponse>(JsonOptions, cancellationToken)
            ?? throw new ChatProviderException(HttpStatusCode.BadGateway, "DeepSeek returned an empty response.");

        var choice = payload.Choices.FirstOrDefault()
            ?? throw new ChatProviderException(HttpStatusCode.BadGateway, "DeepSeek returned no choices.");

        return new ChatResponse(new ChatMessage(ToChatRole(choice.Message.Role), choice.Message.Content ?? ""))
        {
            ResponseId = payload.Id,
            ModelId = payload.Model,
            CreatedAt = payload.Created is null ? null : DateTimeOffset.FromUnixTimeSeconds(payload.Created.Value),
            FinishReason = string.IsNullOrWhiteSpace(choice.FinishReason) ? null : new ChatFinishReason(choice.FinishReason),
            Usage = payload.Usage is null
                ? null
                : new UsageDetails
                {
                    InputTokenCount = payload.Usage.PromptTokens,
                    OutputTokenCount = payload.Usage.CompletionTokens,
                    TotalTokenCount = payload.Usage.TotalTokens
                }
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("DeepSeek streaming is not implemented in S5.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(DeepSeekChatClient) ? this : null;
    }

    public void Dispose()
    {
    }

    private DeepSeekRequest CreateRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var model = string.IsNullOrWhiteSpace(options?.ModelId) ? _options.Model : options.ModelId;

        return new DeepSeekRequest(
            model,
            messages.Select(message => new DeepSeekRequestMessage(ToDeepSeekRole(message.Role), message.Text)).ToArray(),
            options?.Temperature,
            options?.MaxOutputTokens,
            new DeepSeekResponseFormat("json_object"));
    }

    private static string ToDeepSeekRole(ChatRole role)
    {
        if (role == ChatRole.System)
        {
            return "system";
        }

        if (role == ChatRole.Assistant)
        {
            return "assistant";
        }

        if (role == ChatRole.Tool)
        {
            return "tool";
        }

        return "user";
    }

    private static ChatRole ToChatRole(string? role)
    {
        return role switch
        {
            "system" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
    }

    private sealed record DeepSeekRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<DeepSeekRequestMessage> Messages,
        [property: JsonPropertyName("temperature")] float? Temperature,
        [property: JsonPropertyName("max_tokens")] int? MaxTokens,
        [property: JsonPropertyName("response_format")] DeepSeekResponseFormat ResponseFormat);

    private sealed record DeepSeekResponseFormat(
        [property: JsonPropertyName("type")] string Type);

    private sealed record DeepSeekRequestMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record DeepSeekResponse(
        string? Id,
        string? Model,
        long? Created,
        IReadOnlyList<DeepSeekChoice> Choices,
        DeepSeekUsage? Usage);

    private sealed record DeepSeekChoice(
        DeepSeekResponseMessage Message,
        [property: JsonPropertyName("finish_reason")] string? FinishReason);

    private sealed record DeepSeekResponseMessage(string? Role, string? Content);

    private sealed record DeepSeekUsage(
        [property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int? CompletionTokens,
        [property: JsonPropertyName("total_tokens")] int? TotalTokens);
}
