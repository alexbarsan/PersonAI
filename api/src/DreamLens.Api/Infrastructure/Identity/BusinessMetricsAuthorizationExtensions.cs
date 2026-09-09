using System.Security.Claims;

namespace DreamLens.Api.Infrastructure.Identity;

public static class BusinessMetricsAuthorizationExtensions
{
    public const string BusinessMetricsAdminPolicy = "BusinessMetricsAdmin";

    public static IServiceCollection AddDreamLensBusinessMetricsAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var adminGroup = configuration["Metrics:AdminGroup"] ?? "dreamlens-metrics-admin";
        var adminSubjects = configuration.GetSection("Metrics:AdminSubjects").Get<string[]>() ?? [];

        services.AddAuthorizationBuilder()
            .AddPolicy(BusinessMetricsAdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.FindAll("cognito:groups").Any(claim => string.Equals(claim.Value, adminGroup, StringComparison.Ordinal))
                    || context.User.FindAll(ClaimTypes.Role).Any(claim => string.Equals(claim.Value, adminGroup, StringComparison.Ordinal))
                    || context.User.FindFirst("sub")?.Value is { } subject && adminSubjects.Contains(subject, StringComparer.Ordinal)));

        return services;
    }
}
