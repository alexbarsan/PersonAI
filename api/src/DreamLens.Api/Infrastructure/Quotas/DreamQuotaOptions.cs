namespace DreamLens.Api.Infrastructure.Quotas;

public sealed class DreamQuotaOptions
{
    public int DailyDreamSubmissions { get; set; } = 5;
}
