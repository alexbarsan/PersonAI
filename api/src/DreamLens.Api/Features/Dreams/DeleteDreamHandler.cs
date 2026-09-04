using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class DeleteDreamHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var dream = await dbContext.Dreams
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.UserSubject == currentUser.Subject,
                cancellationToken);

        if (dream is null)
        {
            return false;
        }

        var facts = await dbContext.DreamFacts
            .Where(fact => fact.DreamId == dream.Id && fact.UserSubject == currentUser.Subject)
            .ToArrayAsync(cancellationToken);
        dbContext.DreamFacts.RemoveRange(facts);
        var feedback = await dbContext.DreamInterpretationFeedback
            .Where(row => row.DreamId == dream.Id && row.UserSubject == currentUser.Subject)
            .ToArrayAsync(cancellationToken);
        dbContext.DreamInterpretationFeedback.RemoveRange(feedback);
        dbContext.Dreams.Remove(dream);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
