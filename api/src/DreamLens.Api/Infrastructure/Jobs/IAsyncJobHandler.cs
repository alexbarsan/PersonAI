namespace DreamLens.Api.Infrastructure.Jobs;

public interface IAsyncJobHandler
{
    Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken);
}
