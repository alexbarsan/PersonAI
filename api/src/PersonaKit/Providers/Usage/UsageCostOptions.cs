namespace PersonaKit.Providers.Usage;

public sealed class UsageCostOptions
{
    public decimal InputCostPerMillionTokens { get; set; }

    public decimal OutputCostPerMillionTokens { get; set; }
}
