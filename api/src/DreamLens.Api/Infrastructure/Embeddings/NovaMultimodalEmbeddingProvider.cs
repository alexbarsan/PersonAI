using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class NovaMultimodalEmbeddingProvider(
    IBedrockEmbeddingRuntime bedrockRuntime,
    IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    public const int SupportedDimensions = 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EmbeddingResult> CreateAsync(
        string input,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.Dimensions != SupportedDimensions)
        {
            throw new InvalidOperationException(
                $"Dream DNA requires {SupportedDimensions}-dimensional Nova embeddings; configured value was {settings.Dimensions}.");
        }

        var requestBody = CreateRequestBody(input, purpose, settings.Dimensions);
        var responseBody = await bedrockRuntime.InvokeModelAsync(settings.Model, requestBody, cancellationToken);
        var values = ReadEmbedding(responseBody);

        if (values.Length != settings.Dimensions)
        {
            throw new InvalidOperationException(
                $"Embedding provider returned {values.Length} dimensions; expected {settings.Dimensions}.");
        }

        return new EmbeddingResult(
            values,
            null,
            "Amazon Bedrock",
            settings.Model,
            values.Length,
            settings.Version,
            EstimateInputTokens(input) * settings.InputCostPerMillionTokensUsd / 1_000_000m);
    }

    public static byte[] CreateRequestBody(string input, EmbeddingPurpose purpose, int dimensions)
    {
        var embeddingPurpose = purpose switch
        {
            EmbeddingPurpose.Index => "GENERIC_INDEX",
            EmbeddingPurpose.TextRetrieval => "TEXT_RETRIEVAL",
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
        };

        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "nova-multimodal-embed-v1",
            taskType = "SINGLE_EMBEDDING",
            singleEmbeddingParams = new
            {
                embeddingPurpose,
                embeddingDimension = dimensions,
                text = new
                {
                    truncationMode = "END",
                    value = input
                }
            }
        }, JsonOptions);
    }

    public static float[] ReadEmbedding(byte[] responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var embeddings = document.RootElement.GetProperty("embeddings");
        if (embeddings.GetArrayLength() != 1)
        {
            throw new InvalidOperationException("Nova returned an unexpected number of embeddings.");
        }

        return embeddings[0].GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
    }

    public static int EstimateInputTokens(string input) =>
        Math.Max(1, (int)Math.Ceiling(Encoding.UTF8.GetByteCount(input) / 4m));
}
