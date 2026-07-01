using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PersonaKit.Providers.DeepSeek;
using PersonaKit.Providers.Resilience;
using PersonaKit.Providers.Usage;

namespace PersonaKit.Providers;

public static class PersonaKitServiceCollectionExtensions
{
    public static IServiceCollection AddPersonaKitDeepSeekChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DeepSeekOptions>(configuration.GetSection("DeepSeek"));
        services.Configure<ChatResilienceOptions>(configuration.GetSection("ChatResilience"));
        services.Configure<UsageCostOptions>(configuration.GetSection("ChatUsageCost"));
        services.TryAddUsageSink();

        services.AddHttpClient<DeepSeekChatClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DeepSeekOptions>>().Value;
            httpClient.BaseAddress = options.BaseUrl;
        });

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<DeepSeekChatClient>();
            var resilienceOptions = serviceProvider.GetRequiredService<IOptions<ChatResilienceOptions>>().Value;
            var costOptions = serviceProvider.GetRequiredService<IOptions<UsageCostOptions>>().Value;
            var sink = serviceProvider.GetRequiredService<IChatUsageSink>();

            return new UsageLoggingChatClient(
                new ResilienceChatClient(provider, resilienceOptions),
                sink,
                costOptions);
        });

        return services;
    }

    private static void TryAddUsageSink(this IServiceCollection services)
    {
        if (services.Any(service => service.ServiceType == typeof(IChatUsageSink)))
        {
            return;
        }

        services.AddSingleton<IChatUsageSink, InMemoryChatUsageSink>();
    }
}
