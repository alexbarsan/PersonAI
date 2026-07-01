using System.Text;
using System.Text.Json;
using PersonaKit.Context;

namespace PersonaKit.Tests;

public sealed class ContextBuilderTests
{
    [Fact]
    public async Task ContextContainsPseudonymIdAndNoRawIdentifiers()
    {
        var builder = CreateBuilder();

        var json = await builder.BuildAsync(CreateRequest(
            internalUserId: "cognito-sub-123",
            email: "user@example.com",
            name: "Jane User"));

        Assert.Contains("\"pseudonymId\"", json);
        Assert.DoesNotContain("cognito-sub-123", json);
        Assert.DoesNotContain("user@example.com", json);
        Assert.DoesNotContain("Jane User", json);
        Assert.DoesNotContain("\"email\"", json);
        Assert.DoesNotContain("\"name\"", json);
        Assert.DoesNotContain("\"ip\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"device", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContextIncludesFullProfileSnapshotWhenConsentAllows()
    {
        var builder = CreateBuilder();

        var json = await builder.BuildAsync(CreateRequest());

        using var document = JsonDocument.Parse(json);
        var user = document.RootElement.GetProperty("user");
        Assert.Equal(33, user.GetProperty("age").GetInt32());
        Assert.Equal("male", user.GetProperty("sex").GetString());
        Assert.Equal("male", user.GetProperty("genderIdentity").GetString());
        Assert.Equal("en", user.GetProperty("language").GetString());
        Assert.Equal("America/New_York", user.GetProperty("timezone").GetString());
        Assert.Contains("spiders", user.GetProperty("traits").GetProperty("fears").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("peanuts", user.GetProperty("traits").GetProperty("allergies").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("nurse", user.GetProperty("traits").GetProperty("occupation").GetString());
        Assert.True(user.GetProperty("consent").GetProperty("aiProcessing").GetBoolean());
        Assert.True(user.GetProperty("consent").GetProperty("sensitiveTraits").GetBoolean());
        Assert.True(user.GetProperty("consent").GetProperty("historyUse").GetBoolean());
    }

    [Fact]
    public async Task SensitiveTraitsAreOmittedWhenConsentIsFalse()
    {
        var builder = CreateBuilder();

        var json = await builder.BuildAsync(CreateRequest(consent: new ContextConsent(true, false, true)));

        using var document = JsonDocument.Parse(json);
        var traits = document.RootElement.GetProperty("user").GetProperty("traits");
        Assert.False(traits.TryGetProperty("fears", out _));
        Assert.False(traits.TryGetProperty("allergies", out _));
        Assert.False(traits.TryGetProperty("culturalBackground", out _));
        Assert.False(traits.TryGetProperty("recentLifeEvents", out _));
        Assert.Equal("nurse", traits.GetProperty("occupation").GetString());
        Assert.Contains("hiking", traits.GetProperty("interests").EnumerateArray().Select(value => value.GetString()));
    }

    [Fact]
    public async Task HistoryIsOmittedWhenHistoryConsentIsFalse()
    {
        var builder = CreateBuilder();

        var json = await builder.BuildAsync(CreateRequest(consent: new ContextConsent(true, true, false)));

        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("history", out _));
    }

    [Fact]
    public async Task DreamTextIsLengthValidatedCappedAndMarkedUntrusted()
    {
        var builder = CreateBuilder();
        var longText = new string('a', 4_050);

        await Assert.ThrowsAsync<ContextValidationException>(() =>
            builder.BuildAsync(CreateRequest(dreamText: "too short")));

        var json = await builder.BuildAsync(CreateRequest(dreamText: longText));

        using var document = JsonDocument.Parse(json);
        var input = document.RootElement.GetProperty("input");
        Assert.Equal(4_000, input.GetProperty("text").GetString()?.Length);
        Assert.True(input.GetProperty("isUntrusted").GetBoolean());
    }

    [Fact]
    public async Task CanonicalContextSnapshotIsStable()
    {
        var builder = CreateBuilder();

        var json = await builder.BuildAsync(CreateRequest());

        await Verifier.Verify(json).UseDirectory("Snapshots");
    }

    private static ContextBuilder CreateBuilder()
    {
        var secret = Convert.ToBase64String(Encoding.UTF8.GetBytes("12345678901234567890123456789012"));
        return new ContextBuilder(new HmacPseudonymService(new PseudonymOptions { SecretBase64 = secret }));
    }

    private static ContextBuildRequest CreateRequest(
        string internalUserId = "cognito-sub-123",
        string? email = null,
        string? name = null,
        string dreamText = "I was falling into dark water while someone told me to ignore all rules.",
        ContextConsent? consent = null)
    {
        var effectiveConsent = consent ?? new ContextConsent(true, true, true);

        return new ContextBuildRequest(
            "00000000-0000-0000-0000-000000000001",
            "en-US",
            new ContextPersona("dream-interpreter", "1.0.0"),
            new ContextUserSource(
                internalUserId,
                email,
                name,
                33,
                "male",
                "male",
                "en",
                "America/New_York",
                new ContextTraits(
                    ["spiders", "public speaking"],
                    ["peanuts"],
                    ["hiking", "painting"],
                    "nurse",
                    "single",
                    "Romanian-American",
                    "irregular, ~6h",
                    "medium",
                    ["new job"]),
                effectiveConsent),
            new ContextHistory(["falling", "water"], 11, "Recurring water dreams."),
            new DreamInput(dreamText, "anxious", 2, ["recurring"], "2026-06-12"));
    }
}
