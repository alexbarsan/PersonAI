namespace DreamLens.Api.Features.Content;

public sealed record DailyDreamContentResponse(
    DateOnly Date,
    string Quote,
    string? Attribution,
    string[] Facts,
    string[] CognitiveFacts);
