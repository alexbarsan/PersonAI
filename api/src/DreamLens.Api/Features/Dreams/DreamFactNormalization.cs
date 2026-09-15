namespace DreamLens.Api.Features.Dreams;

internal static class DreamFactNormalization
{
    public const string Version = "v1";

    public static string Normalize(string value) => string.Join(
        ' ',
        value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        .ToLowerInvariant();
}
