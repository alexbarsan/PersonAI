using System.Security.Claims;

namespace DreamLens.Api.Infrastructure.Identity;

public static class PrivacyAuthorizationExtensions
{
    public const string PrivacyAdminPolicy = "PrivacyAdmin";

    public static IServiceCollection AddDreamLensPrivacyAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var adminGroup = configuration["Privacy:AdminGroup"] ?? "dreamlens-admin";
        var adminSubjects = configuration.GetSection("Privacy:AdminSubjects").Get<string[]>() ?? [];

        services.AddAuthorizationBuilder()
            .AddPolicy(PrivacyAdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.FindAll("cognito:groups").Any(claim => string.Equals(claim.Value, adminGroup, StringComparison.Ordinal))
                    || context.User.FindAll(ClaimTypes.Role).Any(claim => string.Equals(claim.Value, adminGroup, StringComparison.Ordinal))
                    || context.User.FindFirst("sub")?.Value is { } subject && adminSubjects.Contains(subject, StringComparer.Ordinal)));

        return services;
    }
}
