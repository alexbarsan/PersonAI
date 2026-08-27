namespace DreamLens.Api.Infrastructure.Security;

public sealed class DreamLensCorsOptions
{
    public string[] AllowedOrigins { get; init; } = [];
}
