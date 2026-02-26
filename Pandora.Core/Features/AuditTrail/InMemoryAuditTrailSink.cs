using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Pandora.Core.Features.AuditTrail;

public sealed class InMemoryAuditTrailSink : IAuditTrailSink
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();
    private readonly AuditTrailOptions _options;

    public InMemoryAuditTrailSink(AuditTrailOptions options)
    {
        _options = options;
    }

    public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events.Enqueue(auditEvent);
        Trim();
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<AuditEvent> QueryAsync(AuditQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var entry in _events.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Matches(entry, query))
            {
                continue;
            }

            yield return entry;
            await Task.Yield();
        }
    }

    private bool Matches(AuditEvent entry, AuditQuery query)
    {
        if (query.From is { } from && entry.OccurredAt < from)
        {
            return false;
        }

        if (query.To is { } to && entry.OccurredAt > to)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Actor) && !string.Equals(entry.Actor, query.Actor, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Action) && !string.Equals(entry.Action, query.Action, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Resource) && !string.Equals(entry.Resource, query.Resource, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ResourceId) && !string.Equals(entry.ResourceId, query.ResourceId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId) && !string.Equals(entry.CorrelationId, query.CorrelationId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void Trim()
    {
        while (_events.Count > _options.MaxInMemoryEntries && _events.TryDequeue(out _))
        {
        }
    }
}
