using Amazon;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DreamLens.Api.Infrastructure.Embeddings;

public static class EmbeddingServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensEmbeddings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmbeddingOptions>(configuration.GetSection("Embedding"));

        var enabled = configuration.GetValue<bool>("Embedding:Enabled");
        var provider = configuration["Embedding:Provider"] ?? "fake";

        if (enabled && provider.StartsWith("bedrock-", StringComparison.OrdinalIgnoreCase))
        {
            var region = configuration["AWS:Region"]
                ?? configuration["Authentication:Cognito:Region"]
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? "us-east-1";
            services.TryAddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region)));
            services.AddSingleton<IBedrockEmbeddingRuntime, BedrockEmbeddingRuntime>();
            services.AddScoped<IEmbeddingProvider>(serviceProvider => provider.ToLowerInvariant() switch
            {
                "bedrock-nova-multimodal" => ActivatorUtilities.CreateInstance<NovaMultimodalEmbeddingProvider>(serviceProvider),
                "bedrock-titan" => ActivatorUtilities.CreateInstance<TitanEmbeddingProvider>(serviceProvider),
                _ => throw new InvalidOperationException($"Unsupported embedding provider '{provider}'.")
            });
        }
        else
        {
            services.AddSingleton<IEmbeddingProvider, FakeEmbeddingProvider>();
        }

        return services;
    }
}
