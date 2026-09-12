namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class OperationsActionAuditRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TargetId { get; set; }

    public Guid? JobId { get; set; }

    public required string Source { get; set; }

    public required string OperationType { get; set; }

    public required string Action { get; set; }

    public required string AdministratorSubject { get; set; }

    public required string Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
