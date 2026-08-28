namespace DreamLens.Api.Infrastructure.Assets;

public sealed class PrivateAssetOptions
{
    public string BucketName { get; set; } = "";

    public int DownloadUrlMinutes { get; set; } = 10;
}
