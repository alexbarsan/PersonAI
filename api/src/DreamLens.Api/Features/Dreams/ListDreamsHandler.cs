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
                || (dream.Title != null && dream.Title.ToLower().Contains(term))
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

        if (!string.IsNullOrWhiteSpace(query?.From))
        {
            var from = query.From.Trim();
            dreamsQuery = dreamsQuery.Where(dream => dream.OccurredAt != null && string.Compare(dream.OccurredAt, from) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query?.To))
        {
            var to = query.To.Trim();
            dreamsQuery = dreamsQuery.Where(dream => dream.OccurredAt != null && string.Compare(dream.OccurredAt, to) <= 0);
        }

        var page = Math.Max(1, query?.Page ?? 1);
        var pageSize = Math.Clamp(query?.PageSize ?? 25, 1, 50);
        var total = await dreamsQuery.CountAsync(cancellationToken);
        var dreams = await dreamsQuery
            .OrderByDescending(dream => dream.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var mapped = dreams.Select(dream => new DreamJournalItemResponse(
                dream.Id,
                dream.CreatedAt,
                dream.Status,
                DreamTitleGenerator.Create(dream.Title, DreamMapper.ReadSummary(dream), dream.Text),
                DreamMapper.ReadSummary(dream),
                dream.Mood,
                dream.OccurredAt,
                CreateExcerpt(dream.Text)))
            .ToArray();

        return new DreamJournalResponse(mapped, total, page * pageSize < total);
    }

    private static string CreateExcerpt(string text)
    {
        var normalized = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 180 ? normalized : $"{normalized[..177]}...";
    }
}

public sealed record DreamJournalQuery(string? Query, string? Mood, string? Tag, string? From, string? To, int? Page = null, int? PageSize = null);
