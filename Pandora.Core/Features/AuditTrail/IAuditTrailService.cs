namespace Pandora.Core.Features.AuditTrail;

public interface IAuditTrailService
{
    Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AuditEvent> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
