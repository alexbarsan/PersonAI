namespace DreamLens.Api.Infrastructure.Images;

public enum DreamImagePromptMode
{
    Standard,
    Symbolic
}

public sealed record ImagePromptSafetyResult(
    DreamImagePromptMode Mode,
    string Provider,
    string Model,
    IReadOnlyList<string> Categories,
    bool UsedFallback = false);

public interface IImagePromptSafetyClassifier
{
    Task<ImagePromptSafetyResult> ClassifyAsync(string dreamText, CancellationToken cancellationToken);
}

public sealed class FakeImagePromptSafetyClassifier : IImagePromptSafetyClassifier
{
    public Task<ImagePromptSafetyResult> ClassifyAsync(string dreamText, CancellationToken cancellationToken) =>
        Task.FromResult(new ImagePromptSafetyResult(
            DreamImagePromptMode.Standard,
            "Fake",
            "fake-image-prompt-safety-v1",
            []));
}
