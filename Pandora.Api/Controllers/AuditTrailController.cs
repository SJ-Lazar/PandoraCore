using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.AuditTrail;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("audittrail")]
public class AuditTrailController : ControllerBase
{
    private readonly IAuditTrailService _auditTrailService;

    public AuditTrailController(IAuditTrailService auditTrailService)
    {
        _auditTrailService = auditTrailService;
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordAuditEventRequest request, CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            Actor = request.Actor ?? "anonymous",
            Action = request.Action,
            Resource = request.Resource,
            ResourceId = request.ResourceId,
            CorrelationId = request.CorrelationId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            OccurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow,
            Metadata = request.Metadata ?? new Dictionary<string, string?>()
        };

        await _auditTrailService.RecordAsync(auditEvent, cancellationToken);
        return Accepted();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditEvent>>> Get([FromQuery] AuditQuery query, CancellationToken cancellationToken)
    {
        var results = new List<AuditEvent>();

        await foreach (var entry in _auditTrailService.QueryAsync(query, cancellationToken))
        {
            results.Add(entry);
        }

        return Ok(results);
    }
}

public sealed class RecordAuditEventRequest
{
    public string? Actor { get; init; }

    public string Action { get; init; } = string.Empty;

    public string Resource { get; init; } = string.Empty;

    public string? ResourceId { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? OccurredAt { get; init; }

    public Dictionary<string, string?>? Metadata { get; init; }
}
