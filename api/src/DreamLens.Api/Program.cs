using DreamLens.Api.Features.Health;
using DreamLens.Api.Features.Me.GetMe;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDreamLensAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddDreamLensPersistence(builder.Configuration);
builder.Services.AddDreamLensSecurity(builder.Configuration);
builder.Services.AddScoped<GetMeHandler>();

var profileEndpointsEnabled = ProfileEndpointsEnabled(builder.Configuration);
if (profileEndpointsEnabled)
{
    builder.Services.AddScoped<GetProfileHandler>();
    builder.Services.AddScoped<UpdateProfileHandler>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapMeEndpoints();
app.MapProfileEndpoints();

app.Run();

static bool ProfileEndpointsEnabled(IConfiguration configuration)
{
    return !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DreamLensDb"))
        && !string.IsNullOrWhiteSpace(configuration["Encryption:LocalKeyBase64"]);
}

public partial class Program;
