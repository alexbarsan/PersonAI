namespace DreamLens.Api.Features.Dreams;

public sealed class AskDreamsOptions
{
    public int FreeDailyLimit { get; set; } = 0;

    public int PremiumDailyLimit { get; set; } = 3;

    public int RetrievalLimit { get; set; } = 5;

    public int MaxQuestionLength { get; set; } = 300;
}
