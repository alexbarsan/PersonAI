namespace DreamLens.Api.Infrastructure.Images;

public interface IImageGenerator
{
    string Provider { get; }

    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken);
}

public interface IImageGeneratorRegistry
{
    IImageGenerator GetRequired(string provider);
}

public sealed class ImageGeneratorRegistry(IEnumerable<IImageGenerator> generators) : IImageGeneratorRegistry
{
    private readonly IReadOnlyDictionary<string, IImageGenerator> generators = generators
        .ToDictionary(generator => generator.Provider, StringComparer.OrdinalIgnoreCase);

    public IImageGenerator GetRequired(string provider) => generators.TryGetValue(provider, out var generator)
        ? generator
        : throw new InvalidOperationException($"Image provider '{provider}' is not registered.");
}

public sealed record ImageGenerationRequest(string Prompt, string Style, ImageGenerationRoute Route);

public sealed record ImageGenerationResult(byte[] Content, string ContentType, string Provider, string Model);

public sealed class ImageGenerationException(
    string message,
    bool isRetryable,
    string failureKind) : Exception(message)
{
    public bool IsRetryable { get; } = isRetryable;

    public string FailureKind { get; } = failureKind;
}
