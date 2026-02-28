using System.Collections.Generic;

namespace Pandora.Core.Features.WorkItem;

public class WorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkItemState State { get; private set; } = WorkItemState.Created;

    public WorkItem? Parent { get; private set; }
    public List<WorkItem> Children { get; } = new();
    public List<WorkItemComment> Comments { get; } = new();
    public List<WorkItemTag> Tags { get; } = new();
    public List<WorkItemAttachment> Attachments { get; } = new();

    public Guid? AssignedUserId { get; private set; }
    public Guid? AssignedTeamId { get; private set; }

    public bool IsTemplate { get; set; } = false;
    public string? TemplateName { get; set; }

    public List<LineItem> LineItems { get; set; } = new();

    public void AddChild(WorkItem child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void AssignToUser(Guid userId)
    {
        AssignedUserId = userId;
        AssignedTeamId = null;
    }

    public void AssignToTeam(Guid teamId)
    {
        AssignedTeamId = teamId;
        AssignedUserId = null;
    }

    public void AddComment(WorkItemComment comment) => Comments.Add(comment);
    public void AddTag(WorkItemTag tag) => Tags.Add(tag);
    public void AddAttachment(WorkItemAttachment attachment) => Attachments.Add(attachment);

    public bool CanTransitionTo(WorkItemState newState)
    {
        return State switch
        {
            WorkItemState.Created => newState == WorkItemState.Started || newState == WorkItemState.Pending,
            WorkItemState.Started => newState == WorkItemState.Pending || newState == WorkItemState.Complete,
            WorkItemState.Pending => newState == WorkItemState.Started || newState == WorkItemState.Complete,
            WorkItemState.Complete => false,
            _ => false
        };
    }

    public bool TransitionTo(WorkItemState newState)
    {
        if (CanTransitionTo(newState))
        {
            State = newState;
            return true;
        }
        return false;
    }
}
