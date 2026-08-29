using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetDreamFactsHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<DreamFactsResponse?> HandleAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        var dreamExists = await dbContext.Dreams
            .AsNoTracking()
            .AnyAsync(dream => dream.Id == dreamId && dream.UserSubject == currentUser.Subject, cancellationToken);

        if (!dreamExists)
        {
            return null;
        }

        var facts = await dbContext.DreamFacts
            .AsNoTracking()
            .Where(fact => fact.DreamId == dreamId && fact.UserSubject == currentUser.Subject)
            .OrderBy(fact => fact.FactType)
            .ThenBy(fact => fact.DisplayValue)
            .Select(fact => new DreamFactResponse(
                fact.FactType,
                fact.DisplayValue,
                fact.Score,
                fact.ExtractionConfidence,
                fact.SourceSchemaVersion))
            .ToArrayAsync(cancellationToken);

        return new DreamFactsResponse(dreamId, facts);
    }
}
