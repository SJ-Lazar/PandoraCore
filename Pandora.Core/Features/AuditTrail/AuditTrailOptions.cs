namespace Pandora.Core.Features.AuditTrail;

public sealed class AuditTrailOptions
{
    public int MaxInMemoryEntries { get; set; } = 1024;

    public Func<AuditEvent, bool>? Filter { get; set; }
}
