namespace DreamLens.Api.Features.Dreams;

internal static class ImageStyles
{
    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal)
    {
        "3D_ANIMATED_FAMILY_FILM",
        "DESIGN_SKETCH",
        "FLAT_VECTOR_ILLUSTRATION",
        "GRAPHIC_NOVEL_ILLUSTRATION",
        "MAXIMALISM",
        "MIDCENTURY_RETRO",
        "PHOTOREALISM",
        "SOFT_DIGITAL_PAINTING"
    };

    public static string? Normalize(string? style, string defaultStyle)
    {
        var normalized = string.IsNullOrWhiteSpace(style) ? defaultStyle : style.Trim().ToUpperInvariant();
        return Supported.Contains(normalized) ? normalized : null;
    }
}
