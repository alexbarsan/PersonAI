using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PersonaKit.Context;

namespace DreamLens.Api.Features.Privacy;

public sealed class RequestAnonymizationHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IPseudonymService pseudonymService)
{
    public async Task<AnonymizationRequestResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.AnonymizationRequests
            .AsNoTracking()
            .Where(request => request.RequestingUserSubject == currentUser.Subject
                && request.Status == AnonymizationRequestStatuses.Pending)
            .OrderByDescending(request => request.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return PrivacyMapper.Map(existing);
        }

        var request = new AnonymizationRequest
        {
            RequestingUserSubject = currentUser.Subject,
            RequesterPseudonym = pseudonymService.CreatePseudonym(currentUser.Subject)
        };
        dbContext.AnonymizationRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return PrivacyMapper.Map(request);
    }
}
