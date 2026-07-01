using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DreamLens.Api.Infrastructure.Identity;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        }
        else
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => ConfigureJwtBearer(options, configuration));
        }

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, IConfiguration configuration)
    {
        var cognito = configuration.GetSection("Authentication:Cognito").Get<CognitoOptions>() ?? new CognitoOptions();
        var bearer = configuration.GetSection("Authentication:Schemes:Bearer");

        var authority = FirstNonEmpty(
            cognito.Authority,
            BuildCognitoAuthority(cognito),
            bearer["Authority"]);

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        var audiences = ReadAudiences(cognito, bearer);
        if (audiences.Length == 1)
        {
            options.Audience = audiences[0];
        }

        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "cognito:groups";

        if (audiences.Length > 0)
        {
            options.TokenValidationParameters.ValidAudiences = audiences;
        }

        var validIssuer = FirstNonEmpty(bearer["ValidIssuer"], authority);
        if (!string.IsNullOrWhiteSpace(validIssuer))
        {
            options.TokenValidationParameters.ValidIssuer = validIssuer;
        }
    }

    private static string? BuildCognitoAuthority(CognitoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Region) || string.IsNullOrWhiteSpace(options.UserPoolId))
        {
            return null;
        }

        return $"https://cognito-idp.{options.Region}.amazonaws.com/{options.UserPoolId}";
    }

    private static string[] ReadAudiences(CognitoOptions cognito, IConfigurationSection bearer)
    {
        return new[]
            {
                cognito.Audience,
                cognito.ClientId,
                bearer["Audience"],
                bearer["ValidAudience"]
            }
            .Concat(bearer.GetSection("ValidAudiences").Get<string[]>() ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray()!;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private sealed class CognitoOptions
    {
        public string? Authority { get; init; }

        public string? Region { get; init; }

        public string? UserPoolId { get; init; }

        public string? Audience { get; init; }

        public string? ClientId { get; init; }
    }
}
