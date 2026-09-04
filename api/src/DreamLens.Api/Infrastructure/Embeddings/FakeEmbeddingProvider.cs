using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class FakeEmbeddingProvider(IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    public Task<EmbeddingResult> CreateAsync(
        string input,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dimensions = options.Value.Dimensions;
        var values = new float[dimensions];
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = (hash[index % hash.Length] / 255f) * 2f - 1f;
        }

        return Task.FromResult(new EmbeddingResult(
            values,
            null,
            "fake",
            options.Value.Model,
            dimensions,
            options.Value.Version,
            0));
    }
}
