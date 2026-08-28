using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class TitanEmbeddingProvider(
    IAmazonBedrockRuntime bedrockRuntime,
    IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EmbeddingResult> CreateAsync(string input, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var requestJson = JsonSerializer.Serialize(new
        {
            inputText = input,
            dimensions = settings.Dimensions,
            normalize = true
        }, JsonOptions);

        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson));
        var response = await bedrockRuntime.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = settings.Model,
            ContentType = "application/json",
            Accept = "application/json",
            Body = body
        }, cancellationToken);

        using var document = await JsonDocument.ParseAsync(response.Body, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var values = root.GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
        int? tokenCount = root.TryGetProperty("inputTextTokenCount", out var tokenProperty)
            ? tokenProperty.GetInt32()
            : null;

        if (values.Length != settings.Dimensions)
        {
            throw new InvalidOperationException($"Embedding provider returned {values.Length} dimensions; expected {settings.Dimensions}.");
        }

        return new EmbeddingResult(values, tokenCount, "Amazon Bedrock", settings.Model, values.Length, settings.Version);
    }
}
