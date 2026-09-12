using DreamLens.Api.Infrastructure.Monetization;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Images;

public sealed class ImageGenerationOptions
{
    public string PromptVersion { get; set; } = "dream-image-v2";

    public string DefaultStyle { get; set; } = "SOFT_DIGITAL_PAINTING";

    public int FreeDailyLimit { get; set; } = 1;

    public int PremiumDailyLimit { get; set; } = 5;

    public ImageGenerationTierOptions Free { get; set; } = new();

    public ImageGenerationTierOptions Premium { get; set; } = new();
}

public sealed class ImagePromptSafetyOptions
{
    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = "fake";

    public string Model { get; set; } = "omni-moderation-latest";

    public decimal SymbolicCategoryScoreThreshold { get; set; } = 0.10m;
}

public sealed class ImageGenerationTierOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "fake";

    public string Model { get; set; } = "fake-image-v1";

    public int Width { get; set; } = 1024;

    public int Height { get; set; } = 1024;

    public decimal EstimatedCostUsd { get; set; }

    public string Quality { get; set; } = "medium";
}

public sealed record ImageGenerationRoute(
    EntitlementTier Tier,
    bool Enabled,
    string Provider,
    string Model,
    int Width,
    int Height,
    decimal EstimatedCostUsd,
    string Quality);

public interface IImageGenerationRouteResolver
{
    ImageGenerationRoute Resolve(EntitlementTier tier);
}

public sealed class ConfiguredImageGenerationRouteResolver(IOptions<ImageGenerationOptions> options) : IImageGenerationRouteResolver
{
    public ImageGenerationRoute Resolve(EntitlementTier tier)
    {
        var selected = tier == EntitlementTier.Premium ? options.Value.Premium : options.Value.Free;
        return new ImageGenerationRoute(
            tier,
            selected.Enabled,
            selected.Provider,
            selected.Model,
            Math.Clamp(selected.Width, 320, 4096),
            Math.Clamp(selected.Height, 320, 4096),
            selected.EstimatedCostUsd,
            selected.Quality);
    }
}
