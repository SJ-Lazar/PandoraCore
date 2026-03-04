using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.Import;
using Pandora.Core.Features.Workflow;
using Pandora.Core.Features.WorkItem;
using WorkItemEntity = Pandora.Core.Features.WorkItem.WorkItem;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("import")]
public class ImportController : ControllerBase
{
    private readonly Import _import;
    private readonly IWorkItemService _workItemService;
    private readonly InMemoryWorkflowService _workflowService;

    public ImportController(Import import, IWorkItemService workItemService, InMemoryWorkflowService workflowService)
    {
        _import = import;
        _workItemService = workItemService;
        _workflowService = workflowService;
    }

    [HttpPost("workitems/json")]
    public IActionResult ImportWorkItemsFromJson([FromBody] ImportTextRequest request)
    {
        var rows = _import.FromJson<ImportWorkItemRow>(request.Content);
        var importedIds = new List<Guid>(rows.Count);

        foreach (var row in rows)
        {
            var created = _workItemService.Create(row.Name, row.Description);
            importedIds.Add(created.Id);
        }

        return Ok(new ImportResult { Count = importedIds.Count, Ids = importedIds });
    }

    [HttpPost("workitems/csv")]
    public IActionResult ImportWorkItemsFromCsv([FromBody] ImportCsvRequest request)
    {
        var rows = _import.FromCsv<ImportWorkItemRow>(request.Content, request.Delimiter, request.HasHeader);
        var importedIds = new List<Guid>(rows.Count);

        foreach (var row in rows)
        {
            var created = _workItemService.Create(row.Name, row.Description);
            importedIds.Add(created.Id);
        }

        return Ok(new ImportResult { Count = importedIds.Count, Ids = importedIds });
    }

    [HttpPost("workflows/json")]
    public IActionResult ImportWorkflowsFromJson([FromBody] ImportTextRequest request)
    {
        var rows = _import.FromJson<ImportWorkflowRow>(request.Content);
        var importedIds = new List<Guid>(rows.Count);

        foreach (var row in rows)
        {
            var template = new WorkflowTemplate
            {
                Name = row.Name,
                Steps = row.Steps.Select(s => new WorkflowStep
                {
                    Name = s.Name,
                    Rules = s.Rules ?? new List<string>(),
                    ApproverUserIds = s.ApproverUserIds ?? new List<Guid>(),
                    SlaDuration = s.SlaSeconds.HasValue ? TimeSpan.FromSeconds(s.SlaSeconds.Value) : null,
                    EscalationUserIds = s.EscalationUserIds ?? new List<Guid>(),
                    AutoReassignOnEscalation = s.AutoReassignOnEscalation
                }).ToList()
            };

            var workItems = row.WorkItems.Select(w => new WorkItemEntity
            {
                Name = w.Name,
                Description = w.Description
            }).ToList();

            var created = _workflowService.CreateWorkflow(template, workItems);
            importedIds.Add(created.Id);
        }

        return Ok(new ImportResult { Count = importedIds.Count, Ids = importedIds });
    }
}

public sealed class ImportTextRequest
{
    public string Content { get; init; } = string.Empty;
}

public sealed class ImportCsvRequest
{
    public string Content { get; init; } = string.Empty;

    public string? Delimiter { get; init; }

    public bool HasHeader { get; init; } = true;
}

public sealed class ImportWorkItemRow
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed class ImportWorkflowRow
{
    public string Name { get; init; } = string.Empty;

    public List<ImportWorkflowStepRow> Steps { get; init; } = new();

    public List<ImportWorkItemRow> WorkItems { get; init; } = new();
}

public sealed class ImportWorkflowStepRow
{
    public string Name { get; init; } = string.Empty;

    public List<string>? Rules { get; init; }

    public List<Guid>? ApproverUserIds { get; init; }

    public double? SlaSeconds { get; init; }

    public List<Guid>? EscalationUserIds { get; init; }

    public bool AutoReassignOnEscalation { get; init; }
}

public sealed class ImportResult
{
    public int Count { get; init; }

    public List<Guid> Ids { get; init; } = new();
}
