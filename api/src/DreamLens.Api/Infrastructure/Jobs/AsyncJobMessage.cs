namespace DreamLens.Api.Infrastructure.Jobs;

public sealed record AsyncJobMessage(
    Guid JobId,
    string JobType,
    string UserSubject,
    string PayloadJson);
