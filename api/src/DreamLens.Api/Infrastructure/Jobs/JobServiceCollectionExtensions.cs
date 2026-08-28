using Amazon;
using Amazon.SQS;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Infrastructure.Jobs;

public static class JobServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AsyncJobOptions>(configuration.GetSection("Jobs"));
        services.Configure<AsyncJobWorkerOptions>(configuration.GetSection("Jobs:Worker"));
        var region = configuration["AWS:Region"]
            ?? configuration["Authentication:Cognito:Region"]
            ?? Environment.GetEnvironmentVariable("AWS_REGION")
            ?? "us-east-1";

        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(RegionEndpoint.GetBySystemName(region)));
        services.AddScoped<IAsyncJobQueue, SqsAsyncJobQueue>();

        if (!string.IsNullOrWhiteSpace(PersistenceServiceCollectionExtensions.ResolveConnectionString(configuration)))
        {
            services.AddScoped<AsyncJobService>();
            services.AddScoped<IAsyncJobHandler, DreamEmbeddingJobHandler>();
        }

        services.AddHostedService<AsyncJobWorker>();

        return services;
    }
}
