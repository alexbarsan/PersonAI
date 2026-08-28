using Amazon;
using Amazon.S3;

namespace DreamLens.Api.Infrastructure.Assets;

public static class AssetServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensAssets(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PrivateAssetOptions>(configuration.GetSection("Assets"));
        var region = configuration["AWS:Region"]
            ?? configuration["Authentication:Cognito:Region"]
            ?? Environment.GetEnvironmentVariable("AWS_REGION")
            ?? "us-east-1";

        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(RegionEndpoint.GetBySystemName(region)));
        services.AddScoped<IPrivateAssetStore, S3PrivateAssetStore>();
        return services;
    }
}
