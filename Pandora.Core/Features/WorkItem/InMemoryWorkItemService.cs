using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pandora.Core.Features.WorkItem;

public class InMemoryWorkItemService : IWorkItemService
{
private readonly ConcurrentDictionary<Guid, WorkItem> _items = new();
private readonly List<(Guid WorkItemId, WorkItemState OldState, WorkItemState NewState, DateTime Timestamp)> _stateAuditTrail = new();

    public WorkItem Create(string name, string description)
    {
        var item = new WorkItem { Name = name, Description = description };
        _items[item.Id] = item;
        return item;
    }

    public WorkItem? GetById(Guid id) => _items.TryGetValue(id, out var item) ? item : null;

    public IEnumerable<WorkItem> GetAll() => _items.Values;

    public bool Update(WorkItem workItem)
    {
        if (!_items.ContainsKey(workItem.Id)) return false;
        _items[workItem.Id] = workItem;
        return true;
    }

    public bool Delete(Guid id) => _items.TryRemove(id, out _);

    public void AddChild(Guid parentId, WorkItem child)
    {
        if (_items.TryGetValue(parentId, out var parent))
        {
            parent.AddChild(child);
            _items[child.Id] = child;
        }
    }

    public void AddComment(Guid workItemId, WorkItemComment comment)
    {
        if (_items.TryGetValue(workItemId, out var item))
            item.AddComment(comment);
    }

    public void AddTag(Guid workItemId, WorkItemTag tag)
    {
        if (_items.TryGetValue(workItemId, out var item))
            item.AddTag(tag);
    }

    public void AddAttachment(Guid workItemId, WorkItemAttachment attachment)
    {
        if (_items.TryGetValue(workItemId, out var item))
            item.AddAttachment(attachment);
    }

    public bool TransitionState(Guid workItemId, WorkItemState newState)
    {
        if (_items.TryGetValue(workItemId, out var item))
        {
            if (!item.CanTransitionTo(newState))
                return false;
            var oldState = item.State;
            var transitioned = item.TransitionTo(newState);
            if (transitioned)
            {
                _stateAuditTrail.Add((workItemId, oldState, newState, DateTime.UtcNow));
            }
            return transitioned;
        }
        return false;
    }

    public bool AssignToUser(Guid workItemId, Guid userId)
    {
        if (_items.TryGetValue(workItemId, out var item))
        {
            item.AssignToUser(userId);
            return true;
        }
        return false;
    }

    public bool AssignToTeam(Guid workItemId, Guid teamId)
    {
        if (_items.TryGetValue(workItemId, out var item))
        {
            item.AssignToTeam(teamId);
            return true;
        }
        return false;
    }
}
