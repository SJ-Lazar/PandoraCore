namespace Pandora.Core.Features.AuditTrail;

public sealed record AuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public string Actor { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string Resource { get; init; } = string.Empty;

    public string? ResourceId { get; init; }

    public string? CorrelationId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public IReadOnlyDictionary<string, string?> Metadata { get; init; } = new Dictionary<string, string?>();
}
