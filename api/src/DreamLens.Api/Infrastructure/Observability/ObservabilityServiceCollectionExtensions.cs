namespace DreamLens.Api.Infrastructure.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensObservability(this IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddSingleton(_ => DreamLensMeters.MeterName);
        return services;
    }
}
