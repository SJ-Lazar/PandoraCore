using System;
using System.Collections.Generic;
using Pandora.Core.Features.WorkItem;

namespace Pandora.Core.Features.Workflow;

public enum WorkflowLifecycleState
{
    Created,
    Started,
    InProgress,
    Completed,
    Cancelled
}

public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Pandora.Core.Features.WorkItem.WorkItem> WorkItems { get; set; } = new();
    public Guid TemplateId { get; set; }
    public int CurrentStepIndex { get; set; } = 0;
    public WorkflowLifecycleState LifecycleState { get; set; } = WorkflowLifecycleState.Created;
}

public class WorkflowStep
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkItemId { get; set; }
    public List<Guid> AssignedUserIds { get; set; } = new();
    public List<Guid> AssignedTeamIds { get; set; } = new();
    public List<Guid> ApproverUserIds { get; set; } = new(); // Users who must approve this step
    public bool IsApproved { get; set; } = false;
    public List<string> Rules { get; set; } = new(); // Rule expressions or references
    public TimeSpan? SlaDuration { get; set; }
    public List<Guid> EscalationUserIds { get; set; } = new();
    public bool AutoReassignOnEscalation { get; set; }
}

public class WorkflowTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<string> GlobalRules { get; set; } = new(); // Rules for the whole workflow
}
