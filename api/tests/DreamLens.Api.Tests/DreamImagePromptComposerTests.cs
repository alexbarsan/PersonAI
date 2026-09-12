using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Tests;

public sealed class DreamImagePromptComposerTests
{
    [Fact]
    public void ComposeUsesFactsAndRelevantContextWithoutSendingNamesOrExactLocations()
    {
        var composer = new DreamImagePromptComposer();
        var dream = new DreamRecord { UserSubject = "subject", Text = "I was at work with Alex in Bucharest, carrying a compass through a forest.", Status = "completed" };
        var facts = new[]
        {
            Fact(dream, "person", "Alex"), Fact(dream, "location", "Bucharest"), Fact(dream, "object", "compass"), Fact(dream, "symbol", "forest"), Fact(dream, "theme", "career transition")
        };
        var traits = new ProfileTraitsDto([], [], ["hiking"], "engineer", "single", null, null, "medium", []);

        var prompt = composer.Compose(dream, facts, traits, true, true, new ImagePromptSafetyResult(DreamImagePromptMode.Standard, "OpenAI", "omni", []), "SOFT_DIGITAL_PAINTING", "v2");

        Assert.Equal(DreamImagePromptMode.Standard, prompt.Mode);
        Assert.Contains("compass", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a familiar place", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unnamed familiar figure", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("work-life context", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Alex", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bucharest", prompt.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComposeUsesMetaphorOnlyForSensitiveDreams()
    {
        var composer = new DreamImagePromptComposer();
        var dream = new DreamRecord { UserSubject = "subject", Text = "A violent nightmare", Status = "completed" };
        var facts = new[] { Fact(dream, "object", "knife"), Fact(dream, "theme", "loss") };

        var prompt = composer.Compose(dream, facts, ProfileTraitsDto.Empty, false, false, new ImagePromptSafetyResult(DreamImagePromptMode.Symbolic, "OpenAI", "omni", ["violence"]), "SOFT_DIGITAL_PAINTING", "v2");

        Assert.Equal(DreamImagePromptMode.Symbolic, prompt.Mode);
        Assert.Contains("abstract, symbolic", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("knife", prompt.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("violent nightmare", prompt.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static DreamFactRecord Fact(DreamRecord dream, string type, string value) => new()
    {
        DreamId = dream.Id, UserSubject = dream.UserSubject, FactType = type, NormalizedValue = value.ToLowerInvariant(), DisplayValue = value, SourceSchemaVersion = "test"
    };
}
