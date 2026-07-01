using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetDreamHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<DreamResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var dream = await dbContext.Dreams
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.UserSubject == currentUser.Subject,
                cancellationToken);

        return dream is null ? null : DreamMapper.Map(dream);
    }
}
