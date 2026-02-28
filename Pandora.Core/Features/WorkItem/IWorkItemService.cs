using System.Collections.Generic;

namespace Pandora.Core.Features.WorkItem;

public interface IWorkItemService
{
    WorkItem Create(string name, string description);
    WorkItem? GetById(Guid id);
    IEnumerable<WorkItem> GetAll();
    bool Update(WorkItem workItem);
    bool Delete(Guid id);
    void AddChild(Guid parentId, WorkItem child);
    void AddComment(Guid workItemId, WorkItemComment comment);
    void AddTag(Guid workItemId, WorkItemTag tag);
    void AddAttachment(Guid workItemId, WorkItemAttachment attachment);
    bool TransitionState(Guid workItemId, WorkItemState newState);
}
