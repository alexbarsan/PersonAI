namespace DreamLens.Api.Features.Dreams;

public interface IDreamSubmissionGuard
{
    DreamSubmissionGuardResult Inspect(string text);
}

public sealed record DreamSubmissionGuardResult(bool IsAllowed, string? Message)
{
    public static DreamSubmissionGuardResult Allowed { get; } = new(true, null);

    public static DreamSubmissionGuardResult Rejected(string message) => new(false, message);
}

public sealed class DreamSubmissionGuard : IDreamSubmissionGuard
{
    private static readonly string[] InstructionOverrideIndicators =
    [
        "ignore previous instructions",
        "ignore all previous instructions",
        "ignore the system prompt",
        "override your instructions",
        "disregard previous instructions",
        "reveal your system prompt",
        "show your system prompt",
        "print your system prompt",
        "repeat the system prompt",
        "developer message",
        "jailbreak",
        "prompt injection",
        "act as dan"
    ];

    private static readonly string[] MaliciousRequestIndicators =
    [
        "write malware",
        "create malware",
        "write ransomware",
        "create ransomware",
        "write a keylogger",
        "create a keylogger",
        "steal credentials",
        "steal passwords",
        "phishing email",
        "bypass authentication",
        "exfiltrate data",
        "drop table",
        "delete the database"
    ];

    private static readonly string[] UnrelatedTaskIndicators =
    [
        "write me an email",
        "write an email",
        "write me code",
        "generate code",
        "create a website",
        "summarize this article",
        "translate this text",
        "answer this question",
        "solve this equation",
        "what is the capital of"
    ];

    public DreamSubmissionGuardResult Inspect(string text)
    {
        var normalized = string.Join(' ', text
            .Trim()
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (InstructionOverrideIndicators.Any(normalized.Contains))
        {
            return DreamSubmissionGuardResult.Rejected(
                "This submission looks like an attempt to override Dream DNA's instructions or access protected system information. Enter only the dream you want interpreted.");
        }

        if (MaliciousRequestIndicators.Any(normalized.Contains))
        {
            return DreamSubmissionGuardResult.Rejected(
                "Dream DNA cannot process requests to create malware, steal credentials, damage systems, or facilitate other malicious activity. Enter only a dream you experienced.");
        }

        if (UnrelatedTaskIndicators.Any(normalized.Contains))
        {
            return DreamSubmissionGuardResult.Rejected(
                "This field is only for dreams. Describe what you experienced in the dream instead of asking Dream DNA to perform another task.");
        }

        return DreamSubmissionGuardResult.Allowed;
    }
}
