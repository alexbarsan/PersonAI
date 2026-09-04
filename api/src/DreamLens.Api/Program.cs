using DreamLens.Api.Features.Health;
using DreamLens.Api.Features.Insights;
using DreamLens.Api.Features.Jobs;
using DreamLens.Api.Features.Me.GetMe;
using DreamLens.Api.Features.Entitlements;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Features.Privacy;
using DreamLens.Api.Features.Voice;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Voice;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Observability;
using DreamLens.Api.Infrastructure.OpenApi;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Quotas;
using DreamLens.Api.Infrastructure.RateLimiting;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using PersonaKit.Context;
using PersonaKit.Personas;
using PersonaKit.Pipeline;
using PersonaKit.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDreamLensObservability();
builder.Services.AddDreamLensAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddDreamLensPrivacyAuthorization(builder.Configuration);
builder.Services.AddDreamLensRateLimiting(builder.Configuration);
builder.Services.AddDreamLensPersistence(builder.Configuration);
builder.Services.AddDreamLensEmbeddings(builder.Configuration);
builder.Services.AddDreamLensImageGeneration(builder.Configuration);
builder.Services.AddDreamLensVoiceTranscription(builder.Configuration);
builder.Services.AddDreamLensJobs(builder.Configuration);
builder.Services.AddDreamLensAssets(builder.Configuration);
builder.Services.AddDreamLensSecurity(builder.Configuration);
builder.Services.AddDreamLensMonetization(builder.Configuration);
builder.Services.AddPersonaKitDeepSeekChatClient(builder.Configuration);
AddDreamLensPersonaKitCore(builder.Services, builder.Configuration, builder.Environment);
builder.Services.AddScoped<GetMeHandler>();
builder.Services.AddScoped<GetEntitlementHandler>();

if (!string.IsNullOrWhiteSpace(PersistenceServiceCollectionExtensions.ResolveConnectionString(builder.Configuration)))
{
    builder.Services.AddScoped<IAnonymizedUserAccessService, AnonymizedUserAccessService>();
    builder.Services.AddScoped<GetJobHandler>();
    builder.Services.AddScoped<RetryJobHandler>();
}

var profileEndpointsEnabled = ProfileEndpointsEnabled(builder.Configuration);
if (profileEndpointsEnabled)
{
    builder.Services.AddScoped<GetProfileHandler>();
    builder.Services.AddScoped<UpdateProfileHandler>();
}

var dreamEndpointsEnabled = DreamEndpointsEnabled(builder.Configuration);
if (dreamEndpointsEnabled)
{
    builder.Services.Configure<AskDreamsOptions>(builder.Configuration.GetSection("AskDreams"));
    builder.Services.AddScoped<IDreamQuotaService, EfDreamQuotaService>();
    builder.Services.AddScoped<SubmitDreamHandler>();
    builder.Services.AddScoped<GetDreamHandler>();
    builder.Services.AddScoped<GetDreamFactsHandler>();
    builder.Services.AddScoped<GetSimilarDreamsHandler>();
    builder.Services.AddScoped<GetDreamFeedbackHandler>();
    builder.Services.AddScoped<UpdateDreamFeedbackHandler>();
    builder.Services.AddScoped<AskDreamsHandler>();
    builder.Services.AddScoped<RequestDreamImageHandler>();
    builder.Services.AddScoped<GetDreamImageHandler>();
    builder.Services.AddScoped<ListDreamsHandler>();
    builder.Services.AddScoped<UpdateDreamJournalHandler>();
    builder.Services.AddScoped<DeleteDreamHandler>();
    builder.Services.AddScoped<GetInsightsHandler>();
    builder.Services.AddScoped<SemanticMemoryService>();
    builder.Services.AddScoped<RequestAnonymizationHandler>();
    builder.Services.AddScoped<GetAnonymizationRequestHandler>();
    builder.Services.AddScoped<ListAnonymizationRequestsHandler>();
    builder.Services.AddScoped<ApproveAnonymizationHandler>();
    builder.Services.AddScoped<ExportUserDataHandler>();
    builder.Services.AddScoped<UploadVoiceCaptureHandler>();
    builder.Services.AddScoped<GetVoiceCaptureHandler>();
}

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await ApplyDatabaseMigrationsAsync(app);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDreamLensSwaggerUi();
}

app.UseDreamLensSecurityHeaders();
app.UseCors(SecurityServiceCollectionExtensions.CorsPolicyName);
app.UseAuthentication();
app.UseDreamLensAnonymizedUserGuard();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapMeEndpoints();
app.MapEntitlementEndpoints();
app.MapProfileEndpoints();
app.MapDreamEndpoints();
app.MapInsightsEndpoints();
app.MapJobEndpoints();
app.MapPrivacyEndpoints();
app.MapVoiceEndpoints();

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
    await dbContext.Database.MigrateAsync();
}

static bool ProfileEndpointsEnabled(IConfiguration configuration)
{
    return !string.IsNullOrWhiteSpace(PersistenceServiceCollectionExtensions.ResolveConnectionString(configuration))
        && !string.IsNullOrWhiteSpace(configuration["Encryption:LocalKeyBase64"]);
}

static bool DreamEndpointsEnabled(IConfiguration configuration)
{
    return ProfileEndpointsEnabled(configuration)
        && !string.IsNullOrWhiteSpace(configuration["Pseudonym:SecretBase64"]);
}

static void AddDreamLensPersonaKitCore(
    IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    services.AddSingleton<IPersonaRegistry>(_ => new FilePersonaRegistry(FindPersonasRoot(environment.ContentRootPath)));
    services.AddSingleton<IPromptRenderer, ScribanPromptRenderer>();
    services.AddSingleton<IOutputValidator, JsonSchemaOutputValidator>();
    services.AddSingleton<IResultSectionMapper, SectionMapResultMapper>();
    services.AddSingleton<IModerationPrecheck, NoOpModerationPrecheck>();
    services.AddSingleton<InMemoryInterpretationStore>();
    services.AddSingleton<IInterpretationStore>(serviceProvider => serviceProvider.GetRequiredService<InMemoryInterpretationStore>());
    services.AddSingleton<IAiRunStore>(serviceProvider => serviceProvider.GetRequiredService<InMemoryInterpretationStore>());
    services.AddSingleton<IPseudonymService>(_ => new HmacPseudonymService(new PseudonymOptions
    {
        SecretBase64 = configuration["Pseudonym:SecretBase64"] ?? ""
    }));
    services.AddSingleton<IContextBuilder, ContextBuilder>();
    services.AddScoped<IInterpretationPipeline, InterpretationPipeline>();
}

static string FindPersonasRoot(string contentRootPath)
{
    var directory = new DirectoryInfo(contentRootPath);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "personas");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    return Path.Combine(contentRootPath, "personas");
}

public partial class Program;
