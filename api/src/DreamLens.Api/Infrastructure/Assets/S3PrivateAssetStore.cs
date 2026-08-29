using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Assets;

public sealed class S3PrivateAssetStore(
    IAmazonS3 s3,
    IOptions<PrivateAssetOptions> options) : IPrivateAssetStore
{
    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
        }, cancellationToken);
    }

    public string CreateReadUrl(string key)
    {
        ValidateKey(key);
        return s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.Value.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(Math.Clamp(options.Value.DownloadUrlMinutes, 1, 60)),
            Verb = HttpVerb.GET
        });
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        var response = await s3.GetObjectAsync(options.Value.BucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        await s3.DeleteObjectAsync(options.Value.BucketName, key, cancellationToken);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.StartsWith('/') || key.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Asset key is invalid.", nameof(key));
        }
    }
}
