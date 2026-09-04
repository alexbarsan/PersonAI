namespace DreamLens.Api.Features.Dreams;

public sealed class DeepInterpretationOptions
{
    public bool Enabled { get; set; } = true;

    public string Model { get; set; } = "deepseek-v4-pro";

    public int DailyLimit { get; set; } = 3;

    public int RetrievalLimit { get; set; } = 5;

    public int MaxOutputTokens { get; set; } = 4096;

    public decimal InputCostPerMillionTokensUsd { get; set; } = 1.32m;

    public decimal OutputCostPerMillionTokensUsd { get; set; } = 3.96m;
}
