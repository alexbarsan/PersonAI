using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamJournalSynthesisService(
    DreamLensDbContext dbContext,
    AsyncJobService asyncJobService,
    IOptions<DreamJournalSynthesisOptions> options)
{
    public async Task<int> EnqueueStaleAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return 0;
        }

        var minimum = Math.Clamp(options.Value.MinimumCompletedDreams, 2, 100);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var candidates = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.ConsentAiProcessing && profile.ConsentHistoryUse)
            .Select(profile => new
            {
                profile.Id,
                profile.UserSubject,
                Dreams = dbContext.Dreams.Count(dream => dream.UserSubject == profile.UserSubject && dream.Status == "completed"),
                LatestDreamAt = dbContext.Dreams
                    .Where(dream => dream.UserSubject == profile.UserSubject && dream.Status == "completed")
                    .Max(dream => (DateTimeOffset?)dream.CreatedAt),
                SynthesisLatestDreamAt = dbContext.DreamJournalSyntheses
                    .Where(item => item.UserSubject == profile.UserSubject)
                    .Select(item => (DateTimeOffset?)item.SourceLatestDreamAt)
                    .SingleOrDefault()
            })
            .Where(candidate => candidate.Dreams >= minimum
                && candidate.LatestDreamAt != null
                && (candidate.SynthesisLatestDreamAt == null || candidate.LatestDreamAt > candidate.SynthesisLatestDreamAt))
            .Take(500)
            .ToArrayAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            await EnqueueAsync(candidate.UserSubject, candidate.Id, today, cancellationToken);
        }

        return candidates.Length;
    }

    public async Task EnqueueAfterCompletedDreamAsync(
        string userSubject,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(item => item.UserSubject == userSubject && item.ConsentAiProcessing && item.ConsentHistoryUse)
            .Select(item => new { item.Id })
            .SingleOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            return;
        }

        var completed = await dbContext.Dreams.CountAsync(
            dream => dream.UserSubject == userSubject && dream.Status == "completed",
            cancellationToken);
        if (completed < Math.Clamp(options.Value.MinimumCompletedDreams, 2, 100))
        {
            return;
        }

        await EnqueueAsync(userSubject, profile.Id, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
    }

    private Task<AsyncJobRecord> EnqueueAsync(
        string userSubject,
        Guid profileId,
        DateOnly date,
        CancellationToken cancellationToken) =>
        asyncJobService.EnqueueAsync(
            $"{AsyncJobTypes.DreamJournalSynthesis}:{profileId:N}:{date:yyyyMMdd}",
            AsyncJobTypes.DreamJournalSynthesis,
            userSubject,
            profileId,
            new DreamJournalSynthesisJobHandler.DreamJournalSynthesisJobPayload(options.Value.PromptVersion),
            cancellationToken);
}
