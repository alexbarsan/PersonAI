using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

namespace DreamLens.Api.Infrastructure.Embeddings;

public interface IBedrockEmbeddingRuntime
{
    Task<byte[]> InvokeModelAsync(string modelId, byte[] requestBody, CancellationToken cancellationToken);
}

public sealed class BedrockEmbeddingRuntime(IAmazonBedrockRuntime client) : IBedrockEmbeddingRuntime
{
    public async Task<byte[]> InvokeModelAsync(
        string modelId,
        byte[] requestBody,
        CancellationToken cancellationToken)
    {
        await using var body = new MemoryStream(requestBody, writable: false);
        var response = await client.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = modelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = body
        }, cancellationToken);

        await using var responseBody = response.Body;
        using var buffer = new MemoryStream();
        await responseBody.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
