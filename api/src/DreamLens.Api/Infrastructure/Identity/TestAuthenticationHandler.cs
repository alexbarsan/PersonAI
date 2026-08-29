using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Identity;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers["X-Test-Subject"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", subject),
            new(ClaimTypes.NameIdentifier, subject)
        };

        AddOptionalClaim(claims, "email", ClaimTypes.Email, Request.Headers["X-Test-Email"].FirstOrDefault());
        AddOptionalClaim(claims, "name", ClaimTypes.Name, Request.Headers["X-Test-Name"].FirstOrDefault());
        foreach (var group in Request.Headers["X-Test-Groups"].ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            claims.Add(new Claim("cognito:groups", group));
            claims.Add(new Claim(ClaimTypes.Role, group));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static void AddOptionalClaim(List<Claim> claims, string jwtClaimType, string claimType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        claims.Add(new Claim(jwtClaimType, value));

        if (!string.Equals(jwtClaimType, claimType, StringComparison.Ordinal))
        {
            claims.Add(new Claim(claimType, value));
        }
    }
}
