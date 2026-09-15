using Microsoft.AspNetCore.Http;

namespace DreamLens.Api.Features.Dreams;

public sealed record SubmitDreamResult(
    bool IsValid,
    bool IsCompleted,
    DreamResponse? Dream,
    Dictionary<string, string[]> Errors,
    int ErrorStatusCode = StatusCodes.Status400BadRequest)
{
    public static SubmitDreamResult Accepted(DreamResponse dream)
    {
        return new SubmitDreamResult(true, false, dream, [], StatusCodes.Status202Accepted);
    }

    public static SubmitDreamResult Completed(DreamResponse dream)
    {
        return new SubmitDreamResult(true, true, dream, [], StatusCodes.Status200OK);
    }

    public static SubmitDreamResult Failed(DreamResponse dream)
    {
        return new SubmitDreamResult(true, false, dream, [], StatusCodes.Status503ServiceUnavailable);
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
