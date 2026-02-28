using System.Collections.Concurrent;
using Pandora.Core.Features.WorkItem;

namespace Pandora.Core.Features.Workflow;

public class InMemoryWorkflowService
{
    private readonly ConcurrentDictionary<Guid, Workflow> _workflows = new();
    // Audit trail entry
    public record AuditEntry(DateTime Timestamp, Guid WorkflowId, string Action, Guid? UserId = null, int? StepIndex = null, string? Details = null);
    private readonly List<AuditEntry> _auditTrail = new();

    public enum ApprovalPolicy { All, Any }

    public IEnumerable<AuditEntry> GetAuditTrail(Guid workflowId) => _auditTrail.FindAll(e => e.WorkflowId == workflowId);

    public Workflow CreateWorkflow(WorkflowTemplate template, List<Pandora.Core.Features.WorkItem.WorkItem> workItems)
    {
        if (template.Steps.Count != workItems.Count)
            throw new InvalidOperationException("Number of steps and work items must match template.");

        var workflow = new Workflow
        {
            Name = template.Name,
            TemplateId = template.Id,
            WorkItems = workItems,
            CurrentStepIndex = 0,
            LifecycleState = WorkflowLifecycleState.Created
        };

        _workflows[workflow.Id] = workflow;
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflow.Id, "Created"));
        return workflow;
    }

    public Workflow? GetById(Guid id) => _workflows.TryGetValue(id, out var wf) ? wf : null;
    public IEnumerable<Workflow> GetAll() => _workflows.Values;

    // Reject a step
    public bool RejectStep(Guid workflowId, int stepIndex, Guid userId)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return false;
        var template = GetTemplateForWorkflow(wf);
        if (template == null || stepIndex >= template.Steps.Count) return false;
        var step = template.Steps[stepIndex];
        if (!step.ApproverUserIds.Contains(userId)) return false;
        step.IsApproved = false;
        wf.LifecycleState = WorkflowLifecycleState.Cancelled;
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "StepRejected", userId, stepIndex));
        return true;
    }

    // Skip a step (if allowed by rules)
    public bool SkipStep(Guid workflowId, int stepIndex)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return false;
        var template = GetTemplateForWorkflow(wf);
        if (template == null || stepIndex >= template.Steps.Count) return false;
        var step = template.Steps[stepIndex];
        if (!EvaluateStepRule(step, "CanSkip")) return false;
        wf.CurrentStepIndex++;
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "StepSkipped", null, stepIndex));
        return true;
    }

    // Rule evaluation stub
    public bool EvaluateStepRule(WorkflowStep step, string rule)
    {
        // Implement rule logic as needed
        return step.Rules.Contains(rule);
    }

    // Approve a step by a user
    public bool ApproveStep(Guid workflowId, int stepIndex, Guid userId, ApprovalPolicy policy = ApprovalPolicy.All)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return false;
        var template = GetTemplateForWorkflow(wf);
        if (template == null || stepIndex >= template.Steps.Count) return false;
        var step = template.Steps[stepIndex];
        if (!step.ApproverUserIds.Contains(userId)) return false;
        // Track approvals (simple: set IsApproved for All, or per-user for Any)
        if (policy == ApprovalPolicy.All)
        {
            step.IsApproved = true;
        }
        else
        {
            // For Any, approve if any user approves
            step.IsApproved = true;
        }
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "StepApproved", userId, stepIndex));
        return true;
    }

    // Advance to next step if approved and valid
    public bool AdvanceStep(Guid workflowId, ApprovalPolicy policy = ApprovalPolicy.All)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return false;
        if (wf.LifecycleState == WorkflowLifecycleState.Completed || wf.LifecycleState == WorkflowLifecycleState.Cancelled) return false;
        var template = GetTemplateForWorkflow(wf);
        if (template == null) return false;
        if (wf.CurrentStepIndex >= template.Steps.Count) return false;
        var step = template.Steps[wf.CurrentStepIndex];
        if (!IsStepApproved(step, policy)) return false;
        wf.CurrentStepIndex++;
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "StepAdvanced", null, wf.CurrentStepIndex));
        if (wf.CurrentStepIndex >= template.Steps.Count)
        {
            wf.LifecycleState = WorkflowLifecycleState.Completed;
            _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "Completed"));
        }
        else
        {
            wf.LifecycleState = WorkflowLifecycleState.InProgress;
        }
        return true;
    }

    // Helper: get template for workflow (stub, should be replaced with real lookup)
    private WorkflowTemplate? GetTemplateForWorkflow(Workflow wf)
    {
        // In real implementation, fetch from template store
        return null;
    }

    // Helper: check if step is approved
    private bool IsStepApproved(WorkflowStep step, ApprovalPolicy policy)
    {
        // For All: require IsApproved true
        // For Any: require IsApproved true (extend for per-user approval tracking if needed)
        return step.IsApproved;
    }
}
