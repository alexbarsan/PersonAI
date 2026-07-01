namespace DreamLens.Api.Infrastructure.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EncryptionOptions>(configuration.GetSection("Encryption"));
        services.AddSingleton<IStringEncryptor, AesGcmStringEncryptor>();

        return services;
    }
}
