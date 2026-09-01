namespace DreamLens.Api.Features.Dreams;

public sealed record AskDreamsRequest(string? Question);

public sealed record AskDreamsResponse(
    string Answer,
    IReadOnlyList<string> Observations,
    string Caveat,
    IReadOnlyList<AskDreamSourceResponse> Sources,
    int SampleSize);

public sealed record AskDreamSourceResponse(
    Guid Id,
    string Summary,
    string? OccurredAt,
    DateTimeOffset CreatedAt);

public sealed record AskDreamsResult(
    AskDreamsResponse? Response,
    int StatusCode,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static AskDreamsResult Success(AskDreamsResponse response) =>
        new(response, StatusCodes.Status200OK, new Dictionary<string, string[]>());

    public static AskDreamsResult Failure(int statusCode, string key, string message) =>
        new(null, statusCode, new Dictionary<string, string[]> { [key] = [message] });
}
