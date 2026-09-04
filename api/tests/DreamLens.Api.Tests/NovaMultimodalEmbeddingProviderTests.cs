using System.Text.Json;
using DreamLens.Api.Infrastructure.Embeddings;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Tests;

public sealed class NovaMultimodalEmbeddingProviderTests
{
    [Theory]
    [InlineData(EmbeddingPurpose.Index, "GENERIC_INDEX")]
    [InlineData(EmbeddingPurpose.TextRetrieval, "TEXT_RETRIEVAL")]
    public async Task CreateAsyncUsesExpectedNovaPurpose(
        EmbeddingPurpose purpose,
        string expectedPurpose)
    {
        var runtime = new RecordingRuntime();
        var provider = CreateProvider(runtime);

        var result = await provider.CreateAsync("water under moonlight", purpose, CancellationToken.None);

        Assert.Equal(1024, result.Values.Length);
        Assert.Null(result.InputTokens);
        Assert.True(result.EstimatedCostUsd > 0);
        Assert.Equal("amazon.nova-2-multimodal-embeddings-v1:0", result.Model);
        Assert.Equal("2", result.Version);
        using var request = JsonDocument.Parse(runtime.InvokedRequestBody!);
        var root = request.RootElement;
        Assert.Equal("nova-multimodal-embed-v1", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("SINGLE_EMBEDDING", root.GetProperty("taskType").GetString());
        var parameters = root.GetProperty("singleEmbeddingParams");
        Assert.Equal(expectedPurpose, parameters.GetProperty("embeddingPurpose").GetString());
        Assert.Equal(1024, parameters.GetProperty("embeddingDimension").GetInt32());
        Assert.Equal("END", parameters.GetProperty("text").GetProperty("truncationMode").GetString());
        Assert.Equal("water under moonlight", parameters.GetProperty("text").GetProperty("value").GetString());
    }

    [Fact]
    public async Task CreateAsyncRejectsDimensionsThatDoNotMatchPgvectorColumn()
    {
        var runtime = new RecordingRuntime();
        var provider = new NovaMultimodalEmbeddingProvider(
            runtime,
            Options.Create(new EmbeddingOptions
            {
                Model = "amazon.nova-2-multimodal-embeddings-v1:0",
                Dimensions = 384,
                Version = "2"
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAsync(
            "a dream",
            EmbeddingPurpose.Index,
            CancellationToken.None));

        Assert.Contains("1024-dimensional", exception.Message);
        Assert.Null(runtime.InvokedRequestBody);
    }

    private static NovaMultimodalEmbeddingProvider CreateProvider(RecordingRuntime runtime) => new(
        runtime,
        Options.Create(new EmbeddingOptions
        {
            Model = "amazon.nova-2-multimodal-embeddings-v1:0",
            Dimensions = 1024,
            Version = "2"
        }));

    private sealed class RecordingRuntime : IBedrockEmbeddingRuntime
    {
        public byte[]? InvokedRequestBody { get; private set; }

        public Task<byte[]> InvokeModelAsync(
            string modelId,
            byte[] requestBody,
            CancellationToken cancellationToken)
        {
            InvokedRequestBody = requestBody;
            var response = JsonSerializer.SerializeToUtf8Bytes(new
            {
                embeddings = new[]
                {
                    new
                    {
                        embeddingType = "TEXT",
                        embedding = Enumerable.Repeat(0.25f, 1024).ToArray()
                    }
                }
            });
            return Task.FromResult(response);
        }
    }
}
