using Amazon;
using Amazon.BedrockRuntime;

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

        if (enabled && string.Equals(provider, "bedrock-titan", StringComparison.OrdinalIgnoreCase))
        {
            var region = configuration["AWS:Region"]
                ?? configuration["Authentication:Cognito:Region"]
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? "us-east-1";
            services.AddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region)));
            services.AddScoped<IEmbeddingProvider, TitanEmbeddingProvider>();
        }
        else
        {
            services.AddSingleton<IEmbeddingProvider, FakeEmbeddingProvider>();
        }

        return services;
    }
}
