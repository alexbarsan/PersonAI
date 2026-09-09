using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Monetization;

public sealed class QuotaExemptionOptions
{
    public string[] Subjects { get; set; } = [];
}

public sealed class QuotaExemptionService(IOptions<QuotaExemptionOptions> options)
{
    public bool IsExempt(string userSubject) =>
        options.Value.Subjects.Contains(userSubject, StringComparer.Ordinal);
}
