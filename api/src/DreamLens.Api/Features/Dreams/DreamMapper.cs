using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

public static class DreamMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DreamResponse Map(DreamRecord record, DreamResultResponse? result = null)
    {
        result ??= ReadResult(record);

        return new DreamResponse(
            record.Id,
            record.CreatedAt,
            record.Status,
            result,
            record.ErrorMessage,
            record.Mood,
            record.SleepQuality,
            ReadTags(record),
            record.OccurredAt,
            record.JournalNote);
    }

    public static string? ReadSummary(DreamRecord record)
    {
        return ReadResult(record)?.Summary;
    }

    public static string[] ReadTags(DreamRecord record) => string.IsNullOrWhiteSpace(record.TagsJson)
        ? []
        : JsonSerializer.Deserialize<string[]>(record.TagsJson, JsonOptions) ?? [];

    private static DreamResultResponse? ReadResult(DreamRecord record)
    {
        return string.IsNullOrWhiteSpace(record.ResultJson)
            ? null
            : JsonSerializer.Deserialize<DreamResultResponse>(record.ResultJson, JsonOptions);
    }
}
