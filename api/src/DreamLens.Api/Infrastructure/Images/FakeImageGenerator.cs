namespace DreamLens.Api.Infrastructure.Images;

public sealed class FakeImageGenerator : IImageGenerator
{
    private static readonly byte[] Image = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLk5QAAAABJRU5ErkJggg==");

    public Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ImageGenerationResult(Image, "image/png", "Fake", "fake-image-v1"));
    }
}
