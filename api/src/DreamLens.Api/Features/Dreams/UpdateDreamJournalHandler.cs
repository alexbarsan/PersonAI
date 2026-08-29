using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class UpdateDreamJournalHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<UpdateDreamJournalResult> HandleAsync(
        Guid dreamId,
        UpdateDreamJournalRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SleepQuality is < 1 or > 5)
        {
            return UpdateDreamJournalResult.Invalid("Sleep quality must be between 1 and 5.");
        }

        if (request.JournalNote?.Trim().Length > 2000)
        {
            return UpdateDreamJournalResult.Invalid("Journal note must be 2000 characters or fewer.");
        }

        var dream = await dbContext.Dreams.SingleOrDefaultAsync(
            candidate => candidate.Id == dreamId && candidate.UserSubject == currentUser.Subject,
            cancellationToken);
        if (dream is null)
        {
            return UpdateDreamJournalResult.NotFound();
        }

        dream.Mood = Normalize(request.Mood);
        dream.SleepQuality = request.SleepQuality;
        dream.TagsJson = JsonSerializer.Serialize(NormalizeArray(request.Tags));
        dream.OccurredAt = Normalize(request.OccurredAt);
        dream.JournalNote = Normalize(request.JournalNote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return UpdateDreamJournalResult.Updated(DreamMapper.Map(dream));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string[] NormalizeArray(string[]? values) => values?
        .Select(Normalize)
        .Where(value => value is not null)
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(16)
        .ToArray() ?? [];
}

public sealed record UpdateDreamJournalResult(int StatusCode, DreamResponse? Dream, Dictionary<string, string[]>? Errors)
{
    public static UpdateDreamJournalResult Updated(DreamResponse dream) => new(StatusCodes.Status200OK, dream, null);
    public static UpdateDreamJournalResult NotFound() => new(StatusCodes.Status404NotFound, null, null);
    public static UpdateDreamJournalResult Invalid(string error) => new(StatusCodes.Status400BadRequest, null, new Dictionary<string, string[]> { ["journal"] = [error] });
}
