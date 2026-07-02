namespace DreamLens.Api.Infrastructure.Monetization;

public sealed class MonetizationOptions
{
    public int FreeDailyDreamSubmissions { get; set; } = 3;

    public int PremiumDailyDreamSubmissions { get; set; } = 25;

    public string[] PremiumSubjects { get; set; } = [];
}
