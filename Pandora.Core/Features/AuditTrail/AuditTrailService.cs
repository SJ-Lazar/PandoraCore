namespace Pandora.Core.Features.AuditTrail;

public sealed class AuditTrailService : IAuditTrailService
{
    private readonly IAuditTrailSink _sink;
    private readonly AuditTrailOptions _options;

    public AuditTrailService(IAuditTrailSink sink, AuditTrailOptions options)
    {
        _sink = sink;
        _options = options;
    }

    public Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (_options.Filter is { } filter && !filter(auditEvent))
        {
            return Task.CompletedTask;
        }

        var normalized = auditEvent with
        {
            Id = auditEvent.Id == Guid.Empty ? Guid.NewGuid() : auditEvent.Id,
            OccurredAt = auditEvent.OccurredAt == default ? DateTimeOffset.UtcNow : auditEvent.OccurredAt,
            Metadata = auditEvent.Metadata ?? new Dictionary<string, string?>()
        };

        return _sink.WriteAsync(normalized, cancellationToken).AsTask();
    }

    public IAsyncEnumerable<AuditEvent> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _sink.QueryAsync(query, cancellationToken);
    }
}
