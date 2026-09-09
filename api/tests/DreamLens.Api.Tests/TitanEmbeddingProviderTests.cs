using System.Text.Json;
using DreamLens.Api.Infrastructure.Embeddings;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Tests;

public sealed class TitanEmbeddingProviderTests
{
    [Fact]
    public async Task CreateAsyncRequestsNormalizedTitanV2EmbeddingAndRecordsUsage()
    {
        var runtime = new RecordingRuntime();
        var provider = new TitanEmbeddingProvider(
            runtime,
            Options.Create(new EmbeddingOptions
            {
                Model = "amazon.titan-embed-text-v2:0",
                Dimensions = 1024,
                Version = "3",
                InputCostPerMillionTokensUsd = 0.02m
            }));

        var result = await provider.CreateAsync("water under moonlight", EmbeddingPurpose.Index, CancellationToken.None);

        Assert.Equal(1024, result.Values.Length);
        Assert.Equal(7, result.InputTokens);
        Assert.Equal("amazon.titan-embed-text-v2:0", result.Model);
        Assert.Equal("3", result.Version);
        Assert.True(result.EstimatedCostUsd > 0);
        using var request = JsonDocument.Parse(runtime.InvokedRequestBody!);
        Assert.Equal("water under moonlight", request.RootElement.GetProperty("inputText").GetString());
        Assert.Equal(1024, request.RootElement.GetProperty("dimensions").GetInt32());
        Assert.True(request.RootElement.GetProperty("normalize").GetBoolean());
    }

    private sealed class RecordingRuntime : IBedrockEmbeddingRuntime
    {
        public byte[]? InvokedRequestBody { get; private set; }

        public Task<byte[]> InvokeModelAsync(string modelId, byte[] requestBody, CancellationToken cancellationToken)
        {
            InvokedRequestBody = requestBody;
            return Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(new
            {
                embedding = Enumerable.Repeat(0.25f, 1024).ToArray(),
                inputTextTokenCount = 7
            }));
        }
    }
}
