using System.Security.Claims;

namespace DreamLens.Api.Infrastructure.Identity;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Subject => FindRequiredClaim("sub", ClaimTypes.NameIdentifier);

    public string? Email => FindOptionalClaim("email", ClaimTypes.Email);

    public string? DisplayName => FindOptionalClaim("name", ClaimTypes.Name);

    public string AuthenticationScheme
    {
        get
        {
            var identity = Principal.Identity;

            if (identity is not { IsAuthenticated: true })
            {
                throw new UnauthorizedAccessException("No authenticated user is available.");
            }

            return identity.AuthenticationType ?? string.Empty;
        }
    }

    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("No current HTTP context is available.");

    private string FindRequiredClaim(params string[] claimTypes)
    {
        return FindOptionalClaim(claimTypes)
            ?? throw new UnauthorizedAccessException("The authenticated user is missing a required subject claim.");
    }

    private string? FindOptionalClaim(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = Principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
