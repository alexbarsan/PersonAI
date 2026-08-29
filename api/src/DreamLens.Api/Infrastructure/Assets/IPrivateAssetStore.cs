namespace DreamLens.Api.Infrastructure.Assets;

public interface IPrivateAssetStore
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string CreateReadUrl(string key);
}
