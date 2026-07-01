using System.Net;
using System.Net.Http.Json;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DreamLens.Api.IntegrationTests;

public sealed class ProfileEndpointTests
{
    [Fact]
    public async Task GetProfileReturnsDefaultProfileForNewUser()
    {
        using var app = CreateProfileApp();
        using var client = app.CreateAuthenticatedClient("subject-a");

        var response = await client.GetAsync("/v1/profile");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Null(profile.Age);
        Assert.Equal("en", profile.Language);
        Assert.False(profile.Consent.AiProcessing);
        Assert.False(profile.Consent.SensitiveTraits);
        Assert.False(profile.Consent.HistoryUse);
    }

    [Fact]
    public async Task PutProfileValidatesAndPersistsProfileForCurrentUser()
    {
        using var app = CreateProfileApp();
        using var client = app.CreateAuthenticatedClient("subject-a");

        var update = CreateValidProfileUpdate();
        var putResponse = await client.PutAsJsonAsync("/v1/profile", update);
        var saved = await putResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        var getResponse = await client.GetAsync("/v1/profile");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.NotNull(fetched);
        Assert.Equal(33, fetched.Age);
        Assert.Equal("male", fetched.Sex);
        Assert.Equal("male", fetched.GenderIdentity);
        Assert.Equal("en", fetched.Language);
        Assert.Equal("America/New_York", fetched.Timezone);
        Assert.Contains("spiders", fetched.Traits.Fears);
        Assert.Contains("peanuts", fetched.Traits.Allergies);
        Assert.Equal("nurse", fetched.Traits.Occupation);
        Assert.True(fetched.Consent.AiProcessing);
        Assert.True(fetched.Consent.SensitiveTraits);
        Assert.True(fetched.Consent.HistoryUse);
    }

    [Fact]
    public async Task PutProfileRejectsInvalidAge()
    {
        using var app = CreateProfileApp();
        using var client = app.CreateAuthenticatedClient("subject-a");

        var update = CreateValidProfileUpdate() with { Age = 4 };

        var response = await client.PutAsJsonAsync("/v1/profile", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProfilesAreIsolatedByCurrentUser()
    {
        using var app = CreateProfileApp();
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");

        await userA.PutAsJsonAsync("/v1/profile", CreateValidProfileUpdate());

        var response = await userB.GetAsync("/v1/profile");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Null(profile.Age);
        Assert.Empty(profile.Traits.Fears);
    }

    [Fact]
    public async Task SensitiveTraitsAreEncryptedAtRest()
    {
        using var app = CreateProfileApp();
        using var client = app.CreateAuthenticatedClient("subject-a");

        await client.PutAsJsonAsync("/v1/profile", CreateValidProfileUpdate());

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
        var stored = await db.UserProfiles.SingleAsync(profile => profile.UserSubject == "subject-a");

        Assert.DoesNotContain("spiders", stored.EncryptedTraitsJson);
        Assert.DoesNotContain("peanuts", stored.EncryptedTraitsJson);
        Assert.DoesNotContain("new job", stored.EncryptedTraitsJson);
    }

    private static ProfileTestApp CreateProfileApp()
    {
        var databaseName = $"profile-tests-{Guid.NewGuid():N}";
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DreamLensDb"] = "Host=localhost;Database=dreamlens_profile_tests;Username=postgres;Password=postgres",
                        ["Encryption:LocalKeyBase64"] = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes("12345678901234567890123456789012"))
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<DreamLensDbContext>>();
                    services.RemoveAll<DreamLensDbContext>();
                    services.AddDbContext<DreamLensDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddScoped<GetProfileHandler>();
                    services.AddScoped<UpdateProfileHandler>();
                });
            });

        return new ProfileTestApp(factory);
    }

    private static ProfileUpdateRequest CreateValidProfileUpdate()
    {
        return new ProfileUpdateRequest(
            33,
            "male",
            "male",
            "en",
            "America/New_York",
            new ProfileTraitsRequest(
                ["spiders", "public speaking"],
                ["peanuts"],
                ["hiking", "painting"],
                "nurse",
                "single",
                "Romanian-American",
                "irregular, ~6h",
                "medium",
                ["new job"]),
            new ConsentRequest(true, true, true));
    }

    private sealed class ProfileTestApp(WebApplicationFactory<Program> factory) : IDisposable
    {
        public IServiceProvider Services => factory.Services;

        public HttpClient CreateAuthenticatedClient(string subject)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
            return client;
        }

        public void Dispose()
        {
            factory.Dispose();
        }
    }

    private sealed record ProfileUpdateRequest(
        int? Age,
        string? Sex,
        string? GenderIdentity,
        string Language,
        string Timezone,
        ProfileTraitsRequest Traits,
        ConsentRequest Consent);

    private sealed record ProfileTraitsRequest(
        string[] Fears,
        string[] Allergies,
        string[] Interests,
        string? Occupation,
        string? RelationshipStatus,
        string? CulturalBackground,
        string? SleepPattern,
        string? StressLevel,
        string[] RecentLifeEvents);

    private sealed record ConsentRequest(bool AiProcessing, bool SensitiveTraits, bool HistoryUse);

    private sealed record ProfileResponse(
        int? Age,
        string? Sex,
        string? GenderIdentity,
        string Language,
        string Timezone,
        ProfileTraitsResponse Traits,
        ConsentResponse Consent);

    private sealed record ProfileTraitsResponse(
        string[] Fears,
        string[] Allergies,
        string[] Interests,
        string? Occupation,
        string? RelationshipStatus,
        string? CulturalBackground,
        string? SleepPattern,
        string? StressLevel,
        string[] RecentLifeEvents);

    private sealed record ConsentResponse(bool AiProcessing, bool SensitiveTraits, bool HistoryUse);
}
