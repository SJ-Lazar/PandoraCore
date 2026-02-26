using System.Runtime.CompilerServices;
using Pandora.Core.Features.AuditTrail;

namespace Pandora.Tests.Features.AuditTrail;

public sealed class AuditTrailServiceTests
{
    [Test]
    public async Task RecordAsync_NormalizesEventAndWrites()
    {
        var options = new AuditTrailOptions();
        var sink = new TestSink();
        var service = new AuditTrailService(sink, options);

        var input = new AuditEvent
        {
            Id = Guid.Empty,
            OccurredAt = default,
            Actor = "user",
            Action = "created",
            Resource = "resource",
            Metadata = null!
        };

        await service.RecordAsync(input);

        Assert.That(sink.Written.Count, Is.EqualTo(1));
        var written = sink.Written[0];

        Assert.That(written.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(written.OccurredAt, Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(written.Metadata, Is.Not.Null);
        Assert.That(written.Metadata, Is.Empty);
        Assert.That(written.Actor, Is.EqualTo(input.Actor));
        Assert.That(written.Action, Is.EqualTo(input.Action));
        Assert.That(written.Resource, Is.EqualTo(input.Resource));
    }

    [Test]
    public async Task RecordAsync_StopsWhenFilterReturnsFalse()
    {
        var options = new AuditTrailOptions
        {
            Filter = e => e.Action != "skip"
        };

        var sink = new TestSink();
        var service = new AuditTrailService(sink, options);

        await service.RecordAsync(new AuditEvent { Action = "skip" });

        Assert.That(sink.Written, Is.Empty);
    }

    [Test]
    public async Task QueryAsync_ReturnsMatchingEventsFromSink()
    {
        var options = new AuditTrailOptions();
        var sink = new InMemoryAuditTrailSink(options);
        var service = new AuditTrailService(sink, options);

        await service.RecordAsync(new AuditEvent { Actor = "alice" });
        await service.RecordAsync(new AuditEvent { Actor = "bob" });

        var query = new AuditQuery { Actor = "alice" };

        var results = new List<AuditEvent>();
        await foreach (var entry in service.QueryAsync(query))
        {
            results.Add(entry);
        }

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Actor, Is.EqualTo("alice"));
    }

    private sealed class TestSink : IAuditTrailSink
    {
        public List<AuditEvent> Written { get; } = new();

        public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Written.Add(auditEvent);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuditEvent> QueryAsync(AuditQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var entry in Written)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entry;
                await Task.Yield();
            }
        }
    }
}
