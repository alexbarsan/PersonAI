using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class ListDreamsHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<DreamJournalResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var dreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject)
            .OrderByDescending(dream => dream.CreatedAt)
            .Select(dream => new DreamJournalItemResponse(
                dream.Id,
                dream.CreatedAt,
                dream.Status,
                DreamMapper.ReadSummary(dream),
                dream.Mood,
                dream.OccurredAt))
            .ToArrayAsync(cancellationToken);

        return new DreamJournalResponse(dreams);
    }
}
