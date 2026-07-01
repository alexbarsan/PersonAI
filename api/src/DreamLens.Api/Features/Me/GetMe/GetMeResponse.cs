namespace DreamLens.Api.Features.Me.GetMe;

public sealed record GetMeResponse(
    string Subject,
    string? Email,
    string? DisplayName,
    string AuthenticationScheme);
