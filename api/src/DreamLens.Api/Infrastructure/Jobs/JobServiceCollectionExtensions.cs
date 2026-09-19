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
        services.Configure<EmbeddingBackfillOptions>(configuration.GetSection("Jobs:EmbeddingBackfill"));
        services.Configure<DreamJournalSynthesisOptions>(configuration.GetSection("JournalSynthesis"));
        var region = configuration["AWS:Region"]
            ?? configuration["Authentication:Cognito:Region"]
            ?? Environment.GetEnvironmentVariable("AWS_REGION")
            ?? "us-east-1";

        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(RegionEndpoint.GetBySystemName(region)));
        services.AddScoped<IAsyncJobQueue, SqsAsyncJobQueue>();
        services.AddScoped<IOperationsQueueMonitor, SqsOperationsQueueMonitor>();

        if (!string.IsNullOrWhiteSpace(PersistenceServiceCollectionExtensions.ResolveConnectionString(configuration)))
        {
            services.AddScoped<AsyncJobService>();
            services.AddScoped<IAsyncJobHandler, DreamEmbeddingJobHandler>();
            services.AddScoped<DreamInterpretationJobHandler>();
            services.AddScoped<IAsyncJobHandler>(serviceProvider => serviceProvider.GetRequiredService<DreamInterpretationJobHandler>());
            services.AddScoped<IAsyncJobHandler, DreamImageSafetyJobHandler>();
            services.AddScoped<IAsyncJobHandler, DreamImageJobHandler>();
            services.AddScoped<IAsyncJobHandler, VoiceTranscriptionJobHandler>();
            services.AddScoped<IAsyncJobHandler, PremiumGrantEmailJobHandler>();
            services.AddScoped<EmbeddingBackfillService>();
            services.AddScoped<DreamJournalSynthesisService>();
            services.AddScoped<IAsyncJobHandler, DreamJournalSynthesisJobHandler>();
        }

        services.AddHostedService<AsyncJobWorker>();
        services.AddHostedService<EmbeddingBackfillWorker>();
        services.AddHostedService<DreamJournalSynthesisScheduler>();

        return services;
    }
}
