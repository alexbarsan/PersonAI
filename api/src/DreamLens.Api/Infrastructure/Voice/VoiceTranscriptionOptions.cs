namespace DreamLens.Api.Infrastructure.Voice;

public sealed class VoiceTranscriptionOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "fake";

    public string Model { get; set; } = "amazon-transcribe-standard";

    public int DailyLimit { get; set; } = 3;

    public int MaxDurationSeconds { get; set; } = 180;

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

    public int PollIntervalSeconds { get; set; } = 5;

    public int MaxWaitSeconds { get; set; } = 240;

    public decimal EstimatedCostPerSecondUsd { get; set; } = 0.0004m;
}
