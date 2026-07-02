namespace DreamLens.Api.Infrastructure.Monetization;

public static class MonetizationServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensMonetization(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonetizationOptions>(configuration.GetSection("Monetization"));
        services.AddSingleton<IEntitlementService, ConfiguredEntitlementService>();
        return services;
    }
}
