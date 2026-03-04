using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.WorkItem;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("workitems")]
public class WorkItemsController : ControllerBase
{
    private readonly IWorkItemService _workItemService;

    public WorkItemsController(IWorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<WorkItem>> GetAll() => Ok(_workItemService.GetAll());

    [HttpGet("{id:guid}")]
    public ActionResult<WorkItem> GetById(Guid id)
    {
        var item = _workItemService.GetById(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public ActionResult<WorkItem> Create([FromBody] CreateWorkItemApiRequest request)
    {
        var created = _workItemService.Create(request.Name, request.Description);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public ActionResult<WorkItem> Update(Guid id, [FromBody] UpdateWorkItemRequest request)
    {
        var existing = _workItemService.GetById(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name ?? existing.Name;
        existing.Description = request.Description ?? existing.Description;

        if (request.IsTemplate.HasValue)
        {
            existing.IsTemplate = request.IsTemplate.Value;
        }

        if (request.TemplateName is not null)
        {
            existing.TemplateName = request.TemplateName;
        }

        if (request.LineItems is not null)
        {
            existing.LineItems = request.LineItems;
        }

        var updated = _workItemService.Update(existing);
        return updated ? Ok(existing) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = _workItemService.Delete(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/children")]
    public IActionResult AddChild(Guid id, [FromBody] CreateWorkItemApiRequest request)
    {
        var child = new WorkItem { Name = request.Name, Description = request.Description };
        _workItemService.AddChild(id, child);
        return Ok(child);
    }

    [HttpPost("{id:guid}/comments")]
    public IActionResult AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var comment = new WorkItemComment { Text = request.Text, Author = request.Author };
        _workItemService.AddComment(id, comment);
        return Ok(comment);
    }

    [HttpPost("{id:guid}/tags")]
    public IActionResult AddTag(Guid id, [FromBody] AddTagRequest request)
    {
        var tag = new WorkItemTag { Name = request.Name };
        _workItemService.AddTag(id, tag);
        return Ok(tag);
    }

    [HttpPost("{id:guid}/attachments")]
    public IActionResult AddAttachment(Guid id, [FromBody] AddAttachmentRequest request)
    {
        var attachment = new WorkItemAttachment { FileName = request.FileName, Data = request.Data };
        _workItemService.AddAttachment(id, attachment);
        return Ok(attachment);
    }

    [HttpPost("{id:guid}/transition")]
    public IActionResult TransitionState(Guid id, [FromBody] TransitionStateRequest request)
    {
        var ok = _workItemService.TransitionState(id, request.NewState);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/assign/user")]
    public IActionResult AssignToUser(Guid id, [FromBody] AssignUserRequest request)
    {
        var ok = _workItemService.AssignToUser(id, request.UserId);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/assign/team")]
    public IActionResult AssignToTeam(Guid id, [FromBody] AssignTeamRequest request)
    {
        var ok = _workItemService.AssignToTeam(id, request.TeamId);
        return ok ? Ok() : BadRequest();
    }
}

public sealed class CreateWorkItemApiRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed class UpdateWorkItemRequest
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public bool? IsTemplate { get; init; }

    public string? TemplateName { get; init; }

    public List<LineItem>? LineItems { get; init; }
}

public sealed class AddCommentRequest
{
    public string Text { get; init; } = string.Empty;

    public string? Author { get; init; }
}

public sealed class AddTagRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed class AddAttachmentRequest
{
    public string FileName { get; init; } = string.Empty;

    public string Data { get; init; } = string.Empty;
}

public sealed class TransitionStateRequest
{
    public WorkItemState NewState { get; init; }
}

public sealed class AssignUserRequest
{
    public Guid UserId { get; init; }
}

public sealed class AssignTeamRequest
{
    public Guid TeamId { get; init; }
}
