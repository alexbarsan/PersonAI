namespace DreamLens.Api.Features.Health;

public interface IDatabaseReadinessProbe
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
