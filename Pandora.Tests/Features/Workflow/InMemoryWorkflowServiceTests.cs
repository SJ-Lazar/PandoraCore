using Pandora.Core.Features.Workflow;
using WorkItemEntity = Pandora.Core.Features.WorkItem.WorkItem;

namespace Pandora.Tests.Features.Workflow;

public sealed class InMemoryWorkflowServiceTests
{
    [Test]
    public void CreateWorkflow_ThenAdvance_NoApprovers_CompletesWorkflow()
    {
        var service = new InMemoryWorkflowService();

        var template = new WorkflowTemplate
        {
            Name = "Simple Flow",
            Steps =
            [
                new WorkflowStep { Name = "Step 1" }
            ]
        };

        var workItems = new List<WorkItemEntity>
        {
            new() { Name = "WI-1", Description = "D1" }
        };

        var workflow = service.CreateWorkflow(template, workItems);
        var advanced = service.AdvanceStep(workflow.Id);
        var stored = service.GetById(workflow.Id);

        Assert.That(advanced, Is.True);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.CurrentStepIndex, Is.EqualTo(1));
        Assert.That(stored.LifecycleState, Is.EqualTo(WorkflowLifecycleState.Completed));
    }

    [Test]
    public void AdvanceStep_AllPolicy_RequiresAllApprovers()
    {
        var service = new InMemoryWorkflowService();
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();

        var template = new WorkflowTemplate
        {
            Name = "Approval Flow",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "Step 1",
                    ApproverUserIds = [approver1, approver2]
                }
            ]
        };

        var workflow = service.CreateWorkflow(template,
        [
            new WorkItemEntity { Name = "WI-1", Description = "D1" }
        ]);

        var approvedByOne = service.ApproveStep(workflow.Id, 0, approver1, InMemoryWorkflowService.ApprovalPolicy.All);
        var advanceAfterOne = service.AdvanceStep(workflow.Id, InMemoryWorkflowService.ApprovalPolicy.All);

        var approvedByTwo = service.ApproveStep(workflow.Id, 0, approver2, InMemoryWorkflowService.ApprovalPolicy.All);
        var advanceAfterAll = service.AdvanceStep(workflow.Id, InMemoryWorkflowService.ApprovalPolicy.All);

        Assert.That(approvedByOne, Is.True);
        Assert.That(advanceAfterOne, Is.False);
        Assert.That(approvedByTwo, Is.True);
        Assert.That(advanceAfterAll, Is.True);
    }

    [Test]
    public void AdvanceStep_AnyPolicy_RequiresSingleApprover()
    {
        var service = new InMemoryWorkflowService();
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();

        var template = new WorkflowTemplate
        {
            Name = "Any Approval Flow",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "Step 1",
                    ApproverUserIds = [approver1, approver2]
                }
            ]
        };

        var workflow = service.CreateWorkflow(template,
        [
            new WorkItemEntity { Name = "WI-1", Description = "D1" }
        ]);

        var approved = service.ApproveStep(workflow.Id, 0, approver1, InMemoryWorkflowService.ApprovalPolicy.Any);
        var advanced = service.AdvanceStep(workflow.Id, InMemoryWorkflowService.ApprovalPolicy.Any);

        Assert.That(approved, Is.True);
        Assert.That(advanced, Is.True);
    }

    [Test]
    public void SkipStep_RequiresCanSkipRule()
    {
        var service = new InMemoryWorkflowService();

        var template = new WorkflowTemplate
        {
            Name = "Skip Flow",
            Steps =
            [
                new WorkflowStep { Name = "Step 1", Rules = ["CanSkip"] },
                new WorkflowStep { Name = "Step 2" }
            ]
        };

        var workflow = service.CreateWorkflow(template,
        [
            new WorkItemEntity { Name = "WI-1", Description = "D1" },
            new WorkItemEntity { Name = "WI-2", Description = "D2" }
        ]);

        var skipped = service.SkipStep(workflow.Id, 0);
        var stored = service.GetById(workflow.Id);

        Assert.That(skipped, Is.True);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.CurrentStepIndex, Is.EqualTo(1));
    }
}
