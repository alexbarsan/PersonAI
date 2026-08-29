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
        if (settings.Enabled && string.Equals(settings.Provider, "bedrock-nova-canvas", StringComparison.OrdinalIgnoreCase))
        {
            var region = configuration["AWS:Region"]
                ?? configuration["Authentication:Cognito:Region"]
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? "us-east-1";
            services.TryAddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region)));
            services.AddScoped<IImageGenerator, NovaCanvasImageGenerator>();
        }
        else
        {
            services.AddScoped<IImageGenerator, FakeImageGenerator>();
        }

        return services;
    }
}
