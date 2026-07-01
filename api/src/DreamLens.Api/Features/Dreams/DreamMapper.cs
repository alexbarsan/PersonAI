using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

internal static class DreamMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DreamResponse Map(DreamRecord record, DreamResultResponse? result = null)
    {
        result ??= string.IsNullOrWhiteSpace(record.ResultJson)
            ? null
            : JsonSerializer.Deserialize<DreamResultResponse>(record.ResultJson, JsonOptions);

        return new DreamResponse(record.Id, record.CreatedAt, record.Status, result, record.ErrorMessage);
    }
}
