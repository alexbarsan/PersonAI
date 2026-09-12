using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.AdminOperations;

public sealed class RequeueAdminJobHandler(
    DreamLensDbContext dbContext,
    AsyncJobService asyncJobService,
    ICurrentUser currentUser)
{
    public async Task<AdminOperationsActionResult> HandleAsync(
        Guid jobId,
        AdminOperationsActionRequest request,
        CancellationToken cancellationToken)
    {
        var reason = ValidateReason(request.Reason);
        if (reason is null) return AdminOperationsActionResult.InvalidReason();

        var job = await asyncJobService.RequeueForOperationsAsync(jobId, cancellationToken);
        if (job is null) return AdminOperationsActionResult.NotRecoverable();

        var audit = CreateAudit(job.Id, job.Id, "job", job.JobType, "requeue", reason, currentUser.Subject);
        dbContext.OperationsActionAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminOperationsActionResult.Success(new AdminOperationsActionResponse(
            job.Id, job.Id, audit.Action, job.Status, audit.CreatedAt));
    }

    internal static string? ValidateReason(string? reason)
    {
        var normalized = reason?.Trim();
        return normalized is { Length: >= 10 and <= 500 } ? normalized : null;
    }

    internal static OperationsActionAuditRecord CreateAudit(
        Guid targetId,
        Guid? jobId,
        string source,
        string operationType,
        string action,
        string reason,
        string administratorSubject) => new()
        {
            TargetId = targetId,
            JobId = jobId,
            Source = source,
            OperationType = operationType,
            Action = action,
            AdministratorSubject = administratorSubject,
            Reason = reason
        };
}

public sealed class AcknowledgeAdminIssueHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<AdminOperationsActionResult> HandleAsync(
        string source,
        Guid targetId,
        AdminOperationsActionRequest request,
        CancellationToken cancellationToken)
    {
        var reason = RequeueAdminJobHandler.ValidateReason(request.Reason);
        if (reason is null) return AdminOperationsActionResult.InvalidReason();

        Guid? jobId;
        string operationType;
        if (source == "job")
        {
            var job = await dbContext.AsyncJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == targetId, cancellationToken);
            if (job is null) return AdminOperationsActionResult.NotFound();
            jobId = job.Id;
            operationType = job.JobType;
        }
        else if (source == "image-safety")
        {
            var item = await dbContext.DreamImageSafety.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == targetId, cancellationToken);
            if (item is null) return AdminOperationsActionResult.NotFound();
            jobId = await dbContext.AsyncJobs.AsNoTracking()
                .Where(job => job.TargetId == item.Id && job.JobType == AsyncJobTypes.DreamImageSafety)
                .Select(job => (Guid?)job.Id)
                .SingleOrDefaultAsync(cancellationToken);
            operationType = AsyncJobTypes.DreamImageSafety;
        }
        else if (source == "dream")
        {
            var dreamExists = await dbContext.Dreams.AsNoTracking().AnyAsync(candidate => candidate.Id == targetId, cancellationToken);
            if (!dreamExists) return AdminOperationsActionResult.NotFound();
            jobId = null;
            operationType = "dream.interpretation";
        }
        else
        {
            return AdminOperationsActionResult.NotFound();
        }

        var audit = RequeueAdminJobHandler.CreateAudit(
            targetId, jobId, source, operationType, "acknowledge", reason, currentUser.Subject);
        dbContext.OperationsActionAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminOperationsActionResult.Success(new AdminOperationsActionResponse(
            targetId, jobId, audit.Action, "acknowledged", audit.CreatedAt));
    }
}

public sealed record AdminOperationsActionResult(
    int StatusCode,
    AdminOperationsActionResponse? Response,
    Dictionary<string, string[]>? Errors)
{
    public static AdminOperationsActionResult Success(AdminOperationsActionResponse response) => new(200, response, null);
    public static AdminOperationsActionResult InvalidReason() => new(400, null, new() { ["reason"] = ["Reason must be between 10 and 500 characters."] });
    public static AdminOperationsActionResult NotFound() => new(404, null, null);
    public static AdminOperationsActionResult NotRecoverable() => new(409, null, new() { ["job"] = ["Job is not failed or stale and cannot be requeued."] });
}
