using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class ListDreamsHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<DreamJournalResponse> HandleAsync(DreamJournalQuery? query, CancellationToken cancellationToken)
    {
        var dreamsQuery = dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject);
        if (!string.IsNullOrWhiteSpace(query?.Query))
        {
            var term = query.Query.Trim().ToLowerInvariant();
            dreamsQuery = dreamsQuery.Where(dream => dream.Text.ToLower().Contains(term)
                || (dream.ResultJson != null && dream.ResultJson.ToLower().Contains(term))
                || (dream.JournalNote != null && dream.JournalNote.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query?.Mood))
        {
            var mood = query.Mood.Trim();
            dreamsQuery = dreamsQuery.Where(dream => dream.Mood == mood);
        }

        if (!string.IsNullOrWhiteSpace(query?.Tag))
        {
            var tag = query.Tag.Trim();
            dreamsQuery = dreamsQuery.Where(dream => dream.TagsJson.Contains(tag));
        }

        var dreams = await dreamsQuery
            .OrderByDescending(dream => dream.CreatedAt)
            .Select(dream => new DreamJournalItemResponse(
                dream.Id,
                dream.CreatedAt,
                dream.Status,
                DreamMapper.ReadSummary(dream),
                dream.Mood,
                dream.OccurredAt))
            .ToArrayAsync(cancellationToken);

        var dateFilteredDreams = dreams
            .Where(dream => string.IsNullOrWhiteSpace(query?.From)
                || dream.OccurredAt is not null && string.CompareOrdinal(dream.OccurredAt, query.From) >= 0)
            .Where(dream => string.IsNullOrWhiteSpace(query?.To)
                || dream.OccurredAt is not null && string.CompareOrdinal(dream.OccurredAt, query.To) <= 0)
            .ToArray();
        return new DreamJournalResponse(dateFilteredDreams);
    }
}

public sealed record DreamJournalQuery(string? Query, string? Mood, string? Tag, string? From, string? To);
