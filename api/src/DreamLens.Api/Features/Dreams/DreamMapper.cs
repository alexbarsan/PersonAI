using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

internal static class DreamMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DreamResponse Map(DreamRecord record, DreamResultResponse? result = null)
    {
        result ??= ReadResult(record);

        return new DreamResponse(record.Id, record.CreatedAt, record.Status, result, record.ErrorMessage);
    }

    public static string? ReadSummary(DreamRecord record)
    {
        return ReadResult(record)?.Summary;
    }

    private static DreamResultResponse? ReadResult(DreamRecord record)
    {
        return string.IsNullOrWhiteSpace(record.ResultJson)
            ? null
            : JsonSerializer.Deserialize<DreamResultResponse>(record.ResultJson, JsonOptions);
    }
}
