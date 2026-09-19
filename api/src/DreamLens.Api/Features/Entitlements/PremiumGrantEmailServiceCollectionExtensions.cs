using Amazon;
using Amazon.SimpleEmailV2;

namespace DreamLens.Api.Features.Entitlements;

public static class PremiumGrantEmailServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensPremiumGrantEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PremiumGrantEmailOptions>(configuration.GetSection("PremiumGrantEmail"));
        var settings = configuration.GetSection("PremiumGrantEmail").Get<PremiumGrantEmailOptions>() ?? new PremiumGrantEmailOptions();
        if (settings.Enabled)
        {
            var region = configuration["AWS:Region"]
                ?? configuration["Authentication:Cognito:Region"]
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? "us-east-1";
            services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ => new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(region)));
            services.AddScoped<IPremiumGrantEmailSender, SesPremiumGrantEmailSender>();
        }
        else
        {
            services.AddScoped<IPremiumGrantEmailSender, DisabledPremiumGrantEmailSender>();
        }

        return services;
    }
}
