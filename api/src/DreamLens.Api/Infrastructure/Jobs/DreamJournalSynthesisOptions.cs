namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamJournalSynthesisOptions
{
    public bool Enabled { get; set; } = true;

    public int MinimumCompletedDreams { get; set; } = 6;

    public int MaximumSourceDreams { get; set; } = 24;

    public int SchedulerIntervalMinutes { get; set; } = 360;

    public string Model { get; set; } = "deepseek-v4-pro";

    public int MaxOutputTokens { get; set; } = 3000;

    public decimal InputCostPerMillionTokensUsd { get; set; } = 1.32m;

    public decimal OutputCostPerMillionTokensUsd { get; set; } = 3.96m;

    public string PromptVersion { get; set; } = "journal-synthesis-v3";
}
