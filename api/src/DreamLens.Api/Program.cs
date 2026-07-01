using DreamLens.Api.Features.Health;
using DreamLens.Api.Features.Me.GetMe;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDreamLensAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddDreamLensPersistence(builder.Configuration);
builder.Services.AddScoped<GetMeHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapMeEndpoints();

app.Run();

public partial class Program;
