namespace DreamLens.Api.Infrastructure.Jobs;

public interface IAsyncJobHandler
{
    string JobType { get; }

    Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken);
}
