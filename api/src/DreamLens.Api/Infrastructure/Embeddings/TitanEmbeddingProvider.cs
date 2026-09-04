using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class TitanEmbeddingProvider(
    IBedrockEmbeddingRuntime bedrockRuntime,
    IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EmbeddingResult> CreateAsync(
        string input,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var requestBody = JsonSerializer.SerializeToUtf8Bytes(new
        {
            inputText = input,
            dimensions = settings.Dimensions,
            normalize = true
        }, JsonOptions);

        var responseBody = await bedrockRuntime.InvokeModelAsync(settings.Model, requestBody, cancellationToken);
        using var document = JsonDocument.Parse(responseBody);
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

        var estimatedCost = (tokenCount ?? 0) * settings.InputCostPerMillionTokensUsd / 1_000_000m;
        return new EmbeddingResult(
            values,
            tokenCount,
            "Amazon Bedrock",
            settings.Model,
            values.Length,
            settings.Version,
            estimatedCost);
    }
}
