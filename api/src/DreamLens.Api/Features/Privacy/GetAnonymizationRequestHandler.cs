using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Privacy;

public sealed class GetAnonymizationRequestHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<AnonymizationRequestResponse?> HandleAsync(CancellationToken cancellationToken)
    {
        var request = await dbContext.AnonymizationRequests
            .AsNoTracking()
            .Where(candidate => candidate.RequestingUserSubject == currentUser.Subject)
            .OrderByDescending(candidate => candidate.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return request is null ? null : PrivacyMapper.Map(request);
    }
}
