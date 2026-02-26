namespace Pandora.Core.Features.AuditTrail;

public sealed record AuditQuery
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public string? Actor { get; init; }

    public string? Action { get; init; }

    public string? Resource { get; init; }

    public string? ResourceId { get; init; }

    public string? CorrelationId { get; init; }
}
