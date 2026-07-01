using Microsoft.AspNetCore.Http;

namespace DreamLens.Api.Features.Dreams;

public sealed record SubmitDreamResult(
    bool IsValid,
    bool IsCompleted,
    DreamResponse? Dream,
    Dictionary<string, string[]> Errors,
    int ErrorStatusCode = StatusCodes.Status400BadRequest)
{
    public static SubmitDreamResult Valid(DreamResponse dream)
    {
        return new SubmitDreamResult(true, dream.Status == "completed", dream, []);
    }

    public static SubmitDreamResult Invalid(Dictionary<string, string[]> errors)
    {
        return new SubmitDreamResult(false, false, null, errors);
    }

    public static SubmitDreamResult QuotaExceeded()
    {
        return new SubmitDreamResult(
            false,
            false,
            null,
            new Dictionary<string, string[]>
            {
                ["quota_exceeded"] = ["Daily dream submission quota exceeded."]
            },
            StatusCodes.Status429TooManyRequests);
    }
}
