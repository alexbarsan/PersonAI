namespace DreamLens.Api.Infrastructure.Images;

public interface IImageGenerator
{
    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken);
}

public sealed record ImageGenerationRequest(string Prompt, string Style);

public sealed record ImageGenerationResult(byte[] Content, string ContentType, string Provider, string Model);
