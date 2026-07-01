namespace DreamLens.Api.Infrastructure.Quotas;

public interface IDreamQuotaService
{
    Task<bool> CanSubmitDreamAsync(string userSubject, CancellationToken cancellationToken);
}
