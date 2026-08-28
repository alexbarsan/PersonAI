namespace DreamLens.Api.Infrastructure.Jobs;

public interface IAsyncJobQueue
{
    Task PublishAsync(AsyncJobMessage message, CancellationToken cancellationToken);
}
