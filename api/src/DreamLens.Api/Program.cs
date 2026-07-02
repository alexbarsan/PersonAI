using DreamLens.Api.Features.Health;
using DreamLens.Api.Features.Insights;
using DreamLens.Api.Features.Me.GetMe;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Observability;
using DreamLens.Api.Infrastructure.OpenApi;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Quotas;
using DreamLens.Api.Infrastructure.RateLimiting;
using DreamLens.Api.Infrastructure.Security;
using PersonaKit.Context;
using PersonaKit.Personas;
using PersonaKit.Pipeline;
using PersonaKit.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDreamLensObservability();
builder.Services.AddDreamLensAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddDreamLensRateLimiting(builder.Configuration);
builder.Services.AddDreamLensPersistence(builder.Configuration);
builder.Services.AddDreamLensSecurity(builder.Configuration);
builder.Services.AddPersonaKitDeepSeekChatClient(builder.Configuration);
AddDreamLensPersonaKitCore(builder.Services, builder.Configuration, builder.Environment);
builder.Services.Configure<DreamQuotaOptions>(builder.Configuration.GetSection("DreamQuotas"));
builder.Services.AddScoped<GetMeHandler>();

var profileEndpointsEnabled = ProfileEndpointsEnabled(builder.Configuration);
if (profileEndpointsEnabled)
{
    builder.Services.AddScoped<GetProfileHandler>();
    builder.Services.AddScoped<UpdateProfileHandler>();
}

var dreamEndpointsEnabled = DreamEndpointsEnabled(builder.Configuration);
if (dreamEndpointsEnabled)
{
    builder.Services.AddScoped<IDreamQuotaService, EfDreamQuotaService>();
    builder.Services.AddScoped<SubmitDreamHandler>();
    builder.Services.AddScoped<GetDreamHandler>();
    builder.Services.AddScoped<ListDreamsHandler>();
    builder.Services.AddScoped<DeleteDreamHandler>();
    builder.Services.AddScoped<GetInsightsHandler>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDreamLensSwaggerUi();
}

app.UseDreamLensSecurityHeaders();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapMeEndpoints();
app.MapProfileEndpoints();
app.MapDreamEndpoints();
app.MapInsightsEndpoints();

app.Run();

static bool ProfileEndpointsEnabled(IConfiguration configuration)
{
    return !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DreamLensDb"))
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
