using Amazon;
using Amazon.TranscribeService;

namespace DreamLens.Api.Infrastructure.Voice;

public static class VoiceTranscriptionServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensVoiceTranscription(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VoiceTranscriptionOptions>(configuration.GetSection("VoiceTranscription"));
        var options = configuration.GetSection("VoiceTranscription").Get<VoiceTranscriptionOptions>() ?? new VoiceTranscriptionOptions();
        if (options.Enabled && string.Equals(options.Provider, "amazon-transcribe", StringComparison.OrdinalIgnoreCase))
        {
            var region = configuration["AWS:Region"]
                ?? configuration["Authentication:Cognito:Region"]
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? "us-east-1";
            services.AddSingleton<IAmazonTranscribeService>(_ => new AmazonTranscribeServiceClient(RegionEndpoint.GetBySystemName(region)));
            services.AddScoped<IAudioTranscriber, AmazonTranscribeAudioTranscriber>();
        }
        else
        {
            services.AddScoped<IAudioTranscriber, FakeAudioTranscriber>();
        }

        return services;
    }
}
