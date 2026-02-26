namespace Pandora.Core.Features.AuditTrail;

public interface IAuditTrailSink
{
    ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AuditEvent> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
