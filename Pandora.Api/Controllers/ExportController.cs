using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.Export;
using Pandora.Core.Features.Workflow;
using Pandora.Core.Features.WorkItem;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("export")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IWorkItemService _workItemService;
    private readonly InMemoryWorkflowService _workflowService;

    public ExportController(
        IExportService exportService,
        IWorkItemService workItemService,
        InMemoryWorkflowService workflowService)
    {
        _exportService = exportService;
        _workItemService = workItemService;
        _workflowService = workflowService;
    }

    [HttpGet("workitems/json")]
    public IActionResult ExportWorkItemsJson()
    {
        var json = _exportService.ToJson(_workItemService.GetAll());
        return Content(json, "application/json");
    }

    [HttpGet("workitems/csv")]
    public IActionResult ExportWorkItemsCsv([FromQuery] string? delimiter = null, [FromQuery] bool includeHeader = true)
    {
        var csv = _exportService.ToCsv(_workItemService.GetAll(), delimiter, includeHeader);
        return Content(csv, "text/csv");
    }

    [HttpGet("workflows/json")]
    public IActionResult ExportWorkflowsJson()
    {
        var json = _exportService.ToJson(_workflowService.GetAll());
        return Content(json, "application/json");
    }

    [HttpGet("workflows/csv")]
    public IActionResult ExportWorkflowsCsv([FromQuery] string? delimiter = null, [FromQuery] bool includeHeader = true)
    {
        var csv = _exportService.ToCsv(_workflowService.GetAll(), delimiter, includeHeader);
        return Content(csv, "text/csv");
    }
}
