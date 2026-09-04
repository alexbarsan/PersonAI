namespace DreamLens.Api.Features.Dreams;

public sealed record UpdateDreamFeedbackRequest(
    string? Rating,
    string[]? Reasons,
    string? Details);

public sealed record DreamFeedbackResponse(
    string? Rating,
    string[] Reasons,
    string? Details,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateDreamFeedbackResult(
    DreamFeedbackResponse? Feedback,
    int StatusCode,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static UpdateDreamFeedbackResult Success(DreamFeedbackResponse feedback) =>
        new(feedback, StatusCodes.Status200OK, new Dictionary<string, string[]>());

    public static UpdateDreamFeedbackResult Failure(int statusCode, string key, string message) =>
        new(null, statusCode, new Dictionary<string, string[]> { [key] = [message] });
}

public static class DreamFeedbackRatings
{
    public const string Like = "like";
    public const string Dislike = "dislike";
}

public static class DreamFeedbackReasons
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "inaccurate",
        "too-generic",
        "missed-details",
        "wrong-tone",
        "not-useful",
        "other"
    };
}
