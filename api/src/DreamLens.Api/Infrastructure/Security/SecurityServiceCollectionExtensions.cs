namespace DreamLens.Api.Infrastructure.Security;

public static class SecurityServiceCollectionExtensions
{
    public const string CorsPolicyName = "DreamLensCors";

    public static IServiceCollection AddDreamLensSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EncryptionOptions>(configuration.GetSection("Encryption"));
        services.AddSingleton<IStringEncryptor, AesGcmStringEncryptor>();
        services.AddCors(options =>
        {
            var corsOptions = configuration.GetSection("Cors").Get<DreamLensCorsOptions>() ?? new DreamLensCorsOptions();
            var allowedOrigins = corsOptions.AllowedOrigins
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}
