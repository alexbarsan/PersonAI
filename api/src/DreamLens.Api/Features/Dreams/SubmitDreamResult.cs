namespace DreamLens.Api.Features.Dreams;

public sealed record SubmitDreamResult(bool IsValid, bool IsCompleted, DreamResponse? Dream, Dictionary<string, string[]> Errors)
{
    public static SubmitDreamResult Valid(DreamResponse dream)
    {
        return new SubmitDreamResult(true, dream.Status == "completed", dream, []);
    }

    public static SubmitDreamResult Invalid(Dictionary<string, string[]> errors)
    {
        return new SubmitDreamResult(false, false, null, errors);
    }
}
