using System.Text.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PersonaKit.Context;

namespace DreamLens.Api.Features.AdminOperations;

public sealed class SearchAdminDreamsHandler(
    DreamLensDbContext dbContext,
    IPseudonymService pseudonymService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdminDreamSearchResponse> HandleAsync(
        string? query,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var dreams = dbContext.Dreams.AsNoTracking().AsQueryable();
        var normalizedQuery = query?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            dreams = Guid.TryParse(normalizedQuery, out var dreamId)
                ? dreams.Where(dream => dream.Id == dreamId)
                : dreams.Where(dream => dream.Text.ToLower().Contains(normalizedQuery)
                    || dream.TagsJson.ToLower().Contains(normalizedQuery)
                    || dream.ResultJson != null && dream.ResultJson.ToLower().Contains(normalizedQuery));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            dreams = dreams.Where(dream => dream.Status == status.Trim().ToLowerInvariant());
        }

        var total = await dreams.CountAsync(cancellationToken);
        var records = await dreams
            .OrderByDescending(dream => dream.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        var dreamIds = records.Select(dream => dream.Id).ToArray();
        var images = await dbContext.DreamImages.AsNoTracking()
            .Where(image => dreamIds.Contains(image.DreamId))
            .OrderByDescending(image => image.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return new AdminDreamSearchResponse(
            page,
            pageSize,
            total,
            records.Select(dream =>
            {
                var dreamImages = images.Where(image => image.DreamId == dream.Id).ToArray();
                return new AdminDreamSearchItemResponse(
                    dream.Id,
                    pseudonymService.CreatePseudonym(dream.UserSubject),
                    dream.CreatedAt,
                    dream.OccurredAt,
                    dream.Status,
                    dream.Mood,
                    JsonSerializer.Deserialize<string[]>(dream.TagsJson, JsonOptions) ?? [],
                    ReadSummary(dream.ResultJson),
                    dreamImages.Length,
                    dreamImages.FirstOrDefault()?.Status);
            }).ToArray());
    }

    private static string? ReadSummary(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<DreamResultResponse>(resultJson, JsonOptions)?.Summary;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed class AccessAdminDreamHandler(
    DreamLensDbContext dbContext,
    IPrivateAssetStore assetStore,
    IPseudonymService pseudonymService,
    ICurrentUser currentUser)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdminDreamAccessResult> HandleAsync(
        Guid dreamId,
        AdminDreamAccessRequest request,
        CancellationToken cancellationToken)
    {
        var reason = RequeueAdminJobHandler.ValidateReason(request.Reason);
        if (reason is null) return AdminDreamAccessResult.InvalidReason();

        var dream = await dbContext.Dreams.AsNoTracking().SingleOrDefaultAsync(item => item.Id == dreamId, cancellationToken);
        if (dream is null) return AdminDreamAccessResult.NotFound();
        var images = await dbContext.DreamImages.AsNoTracking()
            .Where(image => image.DreamId == dreamId)
            .OrderByDescending(image => image.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var deep = await dbContext.DreamDeepInterpretations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DreamId == dreamId, cancellationToken);
        var audit = RequeueAdminJobHandler.CreateAudit(
            dream.Id,
            null,
            "dream",
            "dream.content-access",
            "access",
            reason,
            currentUser.Subject);
        dbContext.OperationsActionAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminDreamAccessResult.Success(new AdminDreamDetailResponse(
            dream.Id,
            pseudonymService.CreatePseudonym(dream.UserSubject),
            dream.CreatedAt,
            dream.OccurredAt,
            dream.Status,
            dream.Text,
            dream.Mood,
            dream.SleepQuality,
            JsonSerializer.Deserialize<string[]>(dream.TagsJson, JsonOptions) ?? [],
            dream.JournalNote,
            DeserializeResult(dream.ResultJson),
            DeserializeResult(deep?.ResultJson),
            images.Select(image => new AdminDreamImageResponse(
                image.Id,
                image.Status,
                image.Style,
                image.Status == DreamImageStatuses.Completed && !string.IsNullOrWhiteSpace(image.AssetKey)
                    ? assetStore.CreateReadUrl(image.AssetKey)
                    : null,
                image.CreatedAt)).ToArray()));
    }

    private static DreamResultResponse? DeserializeResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<DreamResultResponse>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }
}

public sealed record AdminDreamAccessResult(
    int StatusCode,
    AdminDreamDetailResponse? Response,
    Dictionary<string, string[]>? Errors)
{
    public static AdminDreamAccessResult Success(AdminDreamDetailResponse response) => new(200, response, null);
    public static AdminDreamAccessResult InvalidReason() => new(400, null, new() { ["reason"] = ["Reason must be between 10 and 500 characters."] });
    public static AdminDreamAccessResult NotFound() => new(404, null, null);
}
