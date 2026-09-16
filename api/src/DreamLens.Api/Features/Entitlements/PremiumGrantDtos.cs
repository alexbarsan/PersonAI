namespace DreamLens.Api.Features.Entitlements;

public sealed record PremiumGrantRequest(string Email);

public sealed record PremiumGrantResponse(
    Guid Id,
    string Email,
    string UserSubject,
    DateTimeOffset GrantedAt,
    string GrantedByEmail);
