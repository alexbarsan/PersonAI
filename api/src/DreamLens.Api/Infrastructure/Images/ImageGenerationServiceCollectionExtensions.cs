using Amazon;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DreamLens.Api.Infrastructure.Images;

public static class ImageGenerationServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensImageGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ImageGenerationOptions>(configuration.GetSection("ImageGeneration"));
        var settings = configuration.GetSection("ImageGeneration").Get<ImageGenerationOptions>() ?? new ImageGenerationOptions();
        var needsBedrock = new[] { settings.Free.Provider, settings.Premium.Provider }
            .Any(provider => string.Equals(provider, "bedrock-nova-canvas", StringComparison.OrdinalIgnoreCase));
        if (needsBedrock)
        {
            var region = configuration["AWS:Region"]
                ?? configuration["Authentication:Cognito:Region"]
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? "us-east-1";
            services.TryAddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region)));
            services.AddScoped<IImageGenerator, NovaCanvasImageGenerator>();
        }
        var needsOpenAi = new[] { settings.Free.Provider, settings.Premium.Provider }
            .Any(provider => string.Equals(provider, OpenAiImageGenerator.ProviderName, StringComparison.OrdinalIgnoreCase));
        if (needsOpenAi)
        {
            services.Configure<OpenAiImageOptions>(configuration.GetSection("OpenAI"));
            services.AddHttpClient<OpenAiImageGenerator>(client => client.Timeout = TimeSpan.FromMinutes(2));
            services.AddScoped<IImageGenerator>(serviceProvider => serviceProvider.GetRequiredService<OpenAiImageGenerator>());
        }
        services.AddScoped<IImageGenerator, FakeImageGenerator>();
        services.AddScoped<IImageGeneratorRegistry, ImageGeneratorRegistry>();
        services.AddSingleton<IImageGenerationRouteResolver, ConfiguredImageGenerationRouteResolver>();

        return services;
    }
}
