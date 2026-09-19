namespace DreamLens.Api.Features.Entitlements;

public sealed class PremiumGrantEmailOptions
{
    public bool Enabled { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Dream DNA";

    public string? ReplyToAddress { get; set; }
}
