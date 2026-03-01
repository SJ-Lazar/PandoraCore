using Pandora.Core.Features.WorkItem;
using WorkItemEntity = Pandora.Core.Features.WorkItem.WorkItem;

namespace Pandora.Tests.Features.WorkItem;

public sealed class InMemoryWorkItemServiceTests
{
    [Test]
    public void Create_And_GetById_ReturnsCreatedItem()
    {
        var service = new InMemoryWorkItemService();

        var created = service.Create("Task A", "Description A");
        var fetched = service.GetById(created.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Name, Is.EqualTo("Task A"));
        Assert.That(fetched.Description, Is.EqualTo("Description A"));
    }

    [Test]
    public void TransitionState_ValidThenInvalid_ReturnsExpectedResult()
    {
        var service = new InMemoryWorkItemService();
        var created = service.Create("Task", "Desc");

        var movedToStarted = service.TransitionState(created.Id, WorkItemState.Started);
        var movedBackToCreated = service.TransitionState(created.Id, WorkItemState.Created);

        Assert.That(movedToStarted, Is.True);
        Assert.That(movedBackToCreated, Is.False);
    }

    [Test]
    public void AssignToUser_ThenAssignToTeam_SwitchesAssignment()
    {
        var service = new InMemoryWorkItemService();
        var created = service.Create("Task", "Desc");
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var assignedUser = service.AssignToUser(created.Id, userId);
        var afterUserAssign = service.GetById(created.Id);

        Assert.That(assignedUser, Is.True);
        Assert.That(afterUserAssign, Is.Not.Null);
        Assert.That(afterUserAssign!.AssignedUserId, Is.EqualTo(userId));
        Assert.That(afterUserAssign.AssignedTeamId, Is.Null);

        var assignedTeam = service.AssignToTeam(created.Id, teamId);
        var afterTeamAssign = service.GetById(created.Id);

        Assert.That(assignedTeam, Is.True);
        Assert.That(afterTeamAssign!.AssignedTeamId, Is.EqualTo(teamId));
        Assert.That(afterTeamAssign.AssignedUserId, Is.Null);
    }

    [Test]
    public void AddChild_AddsRelationship_AndStoresChild()
    {
        var service = new InMemoryWorkItemService();
        var parent = service.Create("Parent", "Parent Desc");
        var child = new WorkItemEntity { Name = "Child", Description = "Child Desc" };

        service.AddChild(parent.Id, child);

        var fetchedParent = service.GetById(parent.Id);
        var fetchedChild = service.GetById(child.Id);

        Assert.That(fetchedParent, Is.Not.Null);
        Assert.That(fetchedParent!.Children.Count, Is.EqualTo(1));
        Assert.That(fetchedParent.Children[0].Id, Is.EqualTo(child.Id));
        Assert.That(fetchedChild, Is.Not.Null);
    }
}
