using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.Workflow;
using WorkItemEntity = Pandora.Core.Features.WorkItem.WorkItem;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly InMemoryWorkflowService _workflowService;

    public WorkflowsController(InMemoryWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Workflow>> GetAll() => Ok(_workflowService.GetAll());

    [HttpGet("{id:guid}")]
    public ActionResult<Workflow> GetById(Guid id)
    {
        var workflow = _workflowService.GetById(id);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    [HttpPost]
    public ActionResult<Workflow> Create([FromBody] CreateWorkflowRequest request)
    {
        var template = new WorkflowTemplate
        {
            Name = request.Name,
            Steps = request.Steps.Select(s => new WorkflowStep
            {
                Name = s.Name,
                WorkItemId = s.WorkItemId ?? Guid.Empty,
                AssignedUserIds = s.AssignedUserIds ?? new List<Guid>(),
                AssignedTeamIds = s.AssignedTeamIds ?? new List<Guid>(),
                ApproverUserIds = s.ApproverUserIds ?? new List<Guid>(),
                Rules = s.Rules ?? new List<string>(),
                SlaDuration = s.SlaSeconds.HasValue ? TimeSpan.FromSeconds(s.SlaSeconds.Value) : null,
                EscalationUserIds = s.EscalationUserIds ?? new List<Guid>(),
                AutoReassignOnEscalation = s.AutoReassignOnEscalation
            }).ToList()
        };

        var workItems = request.WorkItems.Select(w => new WorkItemEntity
        {
            Name = w.Name,
            Description = w.Description
        }).ToList();

        var created = _workflowService.CreateWorkflow(template, workItems);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/approve")]
    public IActionResult Approve(Guid id, [FromBody] ApproveStepRequest request)
    {
        var ok = _workflowService.ApproveStep(id, request.StepIndex, request.UserId, request.Policy);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/advance")]
    public IActionResult Advance(Guid id, [FromBody] AdvanceStepRequest? request = null)
    {
        var policy = request?.Policy ?? InMemoryWorkflowService.ApprovalPolicy.All;
        var ok = _workflowService.AdvanceStep(id, policy);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/skip")]
    public IActionResult Skip(Guid id, [FromBody] SkipStepRequest request)
    {
        var ok = _workflowService.SkipStep(id, request.StepIndex);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/reject")]
    public IActionResult Reject(Guid id, [FromBody] RejectStepRequest request)
    {
        var ok = _workflowService.RejectStep(id, request.StepIndex, request.UserId);
        return ok ? Ok() : BadRequest();
    }

    [HttpGet("{id:guid}/deadline")]
    public ActionResult<DateTime?> GetCurrentStepDeadline(Guid id)
    {
        if (_workflowService.GetById(id) is null) return NotFound();
        return Ok(_workflowService.GetCurrentStepDeadline(id));
    }

    [HttpPost("{id:guid}/reminder")]
    public IActionResult SendReminder(Guid id, [FromBody] TimeActionRequest? request = null)
    {
        var now = request?.UtcNow ?? DateTime.UtcNow;
        var ok = _workflowService.TrySendReminderForCurrentStep(id, now);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/escalate")]
    public IActionResult Escalate(Guid id, [FromBody] TimeActionRequest? request = null)
    {
        var now = request?.UtcNow ?? DateTime.UtcNow;
        var ok = _workflowService.TryEscalateCurrentStep(id, now);
        return ok ? Ok() : BadRequest();
    }

    [HttpGet("{id:guid}/audit")]
    public ActionResult<IEnumerable<InMemoryWorkflowService.AuditEntry>> GetAudit(Guid id)
    {
        if (_workflowService.GetById(id) is null) return NotFound();
        return Ok(_workflowService.GetAuditTrail(id));
    }
}

public sealed class CreateWorkflowRequest
{
    public string Name { get; init; } = string.Empty;

    public List<CreateWorkflowStepRequest> Steps { get; init; } = new();

    public List<CreateWorkItemRequest> WorkItems { get; init; } = new();
}

public sealed class CreateWorkflowStepRequest
{
    public string Name { get; init; } = string.Empty;

    public Guid? WorkItemId { get; init; }

    public List<Guid>? AssignedUserIds { get; init; }

    public List<Guid>? AssignedTeamIds { get; init; }

    public List<Guid>? ApproverUserIds { get; init; }

    public List<string>? Rules { get; init; }

    public double? SlaSeconds { get; init; }

    public List<Guid>? EscalationUserIds { get; init; }

    public bool AutoReassignOnEscalation { get; init; }
}

public sealed class CreateWorkItemRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed class ApproveStepRequest
{
    public int StepIndex { get; init; }

    public Guid UserId { get; init; }

    public InMemoryWorkflowService.ApprovalPolicy Policy { get; init; } = InMemoryWorkflowService.ApprovalPolicy.All;
}

public sealed class AdvanceStepRequest
{
    public InMemoryWorkflowService.ApprovalPolicy Policy { get; init; } = InMemoryWorkflowService.ApprovalPolicy.All;
}

public sealed class SkipStepRequest
{
    public int StepIndex { get; init; }
}

public sealed class RejectStepRequest
{
    public int StepIndex { get; init; }

    public Guid UserId { get; init; }
}

public sealed class TimeActionRequest
{
    public DateTime? UtcNow { get; init; }
}
