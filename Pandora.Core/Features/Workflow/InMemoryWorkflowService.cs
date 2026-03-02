using System.Collections.Concurrent;
using Pandora.Core.Features.WorkItem;

namespace Pandora.Core.Features.Workflow;

public class InMemoryWorkflowService
{
    private readonly ConcurrentDictionary<Guid, Workflow> _workflows = new();
    private readonly ConcurrentDictionary<Guid, WorkflowTemplate> _templates = new();
    private readonly ConcurrentDictionary<(Guid WorkflowId, int StepIndex), ConcurrentDictionary<Guid, byte>> _stepApprovals = new();
    private readonly ConcurrentDictionary<(Guid WorkflowId, int StepIndex), DateTime> _stepDeadlines = new();
    private readonly ConcurrentDictionary<(Guid WorkflowId, int StepIndex), byte> _stepRemindersSent = new();
    private readonly ConcurrentDictionary<(Guid WorkflowId, int StepIndex), byte> _stepEscalations = new();
    // Audit trail entry
    public record AuditEntry(DateTime Timestamp, Guid WorkflowId, string Action, Guid? UserId = null, int? StepIndex = null, string? Details = null);
    private readonly List<AuditEntry> _auditTrail = new();

    public enum ApprovalPolicy { All, Any }

    public IEnumerable<AuditEntry> GetAuditTrail(Guid workflowId) => _auditTrail.FindAll(e => e.WorkflowId == workflowId);

    public Workflow CreateWorkflow(WorkflowTemplate template, List<Pandora.Core.Features.WorkItem.WorkItem> workItems)
    {
        if (template.Steps.Count != workItems.Count)
            throw new InvalidOperationException("Number of steps and work items must match template.");

        _templates[template.Id] = template;

        for (var index = 0; index < template.Steps.Count; index++)
        {
            if (template.Steps[index].WorkItemId == Guid.Empty)
            {
                template.Steps[index].WorkItemId = workItems[index].Id;
            }
        }

        var workflow = new Workflow
        {
            Name = template.Name,
            TemplateId = template.Id,
            WorkItems = workItems,
            CurrentStepIndex = 0,
            LifecycleState = WorkflowLifecycleState.Created
        };

        _workflows[workflow.Id] = workflow;
        InitializeStepTiming(workflow.Id, template, workflow.CurrentStepIndex, DateTime.UtcNow);
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
        if (wf.CurrentStepIndex != stepIndex) return false;
        var step = template.Steps[stepIndex];
        if (!EvaluateStepRule(step, "CanSkip")) return false;
        wf.CurrentStepIndex++;
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "StepSkipped", null, stepIndex));

        if (wf.CurrentStepIndex >= template.Steps.Count)
        {
            wf.LifecycleState = WorkflowLifecycleState.Completed;
            _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "Completed"));
        }
        else
        {
            InitializeStepTiming(workflowId, template, wf.CurrentStepIndex, DateTime.UtcNow);
            wf.LifecycleState = WorkflowLifecycleState.InProgress;
        }
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
        if (template == null || stepIndex < 0 || stepIndex >= template.Steps.Count) return false;
        if (wf.CurrentStepIndex != stepIndex) return false;

        var step = template.Steps[stepIndex];
        if (!step.ApproverUserIds.Contains(userId)) return false;

        var approvals = _stepApprovals.GetOrAdd((workflowId, stepIndex), _ => new ConcurrentDictionary<Guid, byte>());
        approvals[userId] = 0;

        step.IsApproved = IsStepApproved(workflowId, step, stepIndex, policy);
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
        if (!IsStepApproved(workflowId, step, wf.CurrentStepIndex, policy)) return false;

        if (wf.LifecycleState == WorkflowLifecycleState.Created)
        {
            wf.LifecycleState = WorkflowLifecycleState.Started;
        }

        wf.CurrentStepIndex++;
        _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "StepAdvanced", null, wf.CurrentStepIndex));

        if (wf.CurrentStepIndex >= template.Steps.Count)
        {
            wf.LifecycleState = WorkflowLifecycleState.Completed;
            _auditTrail.Add(new AuditEntry(DateTime.UtcNow, workflowId, "Completed"));
        }
        else
        {
            InitializeStepTiming(workflowId, template, wf.CurrentStepIndex, DateTime.UtcNow);
            wf.LifecycleState = WorkflowLifecycleState.InProgress;
        }
        return true;
    }

    public DateTime? GetCurrentStepDeadline(Guid workflowId)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return null;
        return _stepDeadlines.TryGetValue((workflowId, wf.CurrentStepIndex), out var deadline)
            ? deadline
            : null;
    }

    public bool TrySendReminderForCurrentStep(Guid workflowId, DateTime utcNow)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return false;
        if (wf.LifecycleState == WorkflowLifecycleState.Completed || wf.LifecycleState == WorkflowLifecycleState.Cancelled) return false;

        var key = (workflowId, wf.CurrentStepIndex);
        if (!_stepDeadlines.TryGetValue(key, out var deadline)) return false;
        if (utcNow < deadline) return false;
        if (!_stepRemindersSent.TryAdd(key, 0)) return false;

        _auditTrail.Add(new AuditEntry(utcNow, workflowId, "StepReminderSent", null, wf.CurrentStepIndex, $"Deadline exceeded at {deadline:o}"));
        return true;
    }

    public bool TryEscalateCurrentStep(Guid workflowId, DateTime utcNow)
    {
        if (!_workflows.TryGetValue(workflowId, out var wf)) return false;
        if (wf.LifecycleState == WorkflowLifecycleState.Completed || wf.LifecycleState == WorkflowLifecycleState.Cancelled) return false;

        var template = GetTemplateForWorkflow(wf);
        if (template == null || wf.CurrentStepIndex >= template.Steps.Count) return false;

        var key = (workflowId, wf.CurrentStepIndex);
        if (!_stepDeadlines.TryGetValue(key, out var deadline)) return false;
        if (utcNow < deadline) return false;
        if (!_stepEscalations.TryAdd(key, 0)) return false;

        var step = template.Steps[wf.CurrentStepIndex];
        if (step.AutoReassignOnEscalation && step.EscalationUserIds.Count > 0 && wf.CurrentStepIndex < wf.WorkItems.Count)
        {
            wf.WorkItems[wf.CurrentStepIndex].AssignToUser(step.EscalationUserIds[0]);
        }

        _auditTrail.Add(new AuditEntry(utcNow, workflowId, "StepEscalated", null, wf.CurrentStepIndex, $"Deadline exceeded at {deadline:o}"));
        return true;
    }

    // Helper: get template for workflow (stub, should be replaced with real lookup)
    private WorkflowTemplate? GetTemplateForWorkflow(Workflow wf)
    {
        return _templates.TryGetValue(wf.TemplateId, out var template) ? template : null;
    }

    // Helper: check if step is approved
    private bool IsStepApproved(Guid workflowId, WorkflowStep step, int stepIndex, ApprovalPolicy policy)
    {
        if (step.ApproverUserIds.Count == 0)
        {
            return true;
        }

        if (!_stepApprovals.TryGetValue((workflowId, stepIndex), out var approvals))
        {
            return false;
        }

        return policy switch
        {
            ApprovalPolicy.All => step.ApproverUserIds.All(approvals.ContainsKey),
            ApprovalPolicy.Any => step.ApproverUserIds.Any(approvals.ContainsKey),
            _ => false
        };
    }

    private void InitializeStepTiming(Guid workflowId, WorkflowTemplate template, int stepIndex, DateTime startedAtUtc)
    {
        if (stepIndex < 0 || stepIndex >= template.Steps.Count) return;

        var step = template.Steps[stepIndex];
        var key = (workflowId, stepIndex);

        _stepRemindersSent.TryRemove(key, out _);
        _stepEscalations.TryRemove(key, out _);

        if (step.SlaDuration.HasValue && step.SlaDuration.Value > TimeSpan.Zero)
        {
            _stepDeadlines[key] = startedAtUtc.Add(step.SlaDuration.Value);
        }
        else
        {
            _stepDeadlines.TryRemove(key, out _);
        }
    }
}
