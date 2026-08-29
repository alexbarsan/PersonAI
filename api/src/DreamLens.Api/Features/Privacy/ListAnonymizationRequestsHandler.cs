using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Privacy;

public sealed class ListAnonymizationRequestsHandler(DreamLensDbContext dbContext)
{
    public async Task<AdminAnonymizationRequestResponse[]> HandleAsync(string? status, CancellationToken cancellationToken)
    {
        var query = dbContext.AnonymizationRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(request => request.Status == status.Trim().ToLowerInvariant());
        }

        return await query
            .OrderBy(request => request.RequestedAt)
            .Select(request => new AdminAnonymizationRequestResponse(
                request.Id,
                request.RequesterPseudonym,
                request.Status,
                request.RequestedAt))
            .ToArrayAsync(cancellationToken);
    }
}
