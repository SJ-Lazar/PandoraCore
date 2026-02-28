namespace Pandora.Core.Features.WorkItem;

public class WorkItemAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public object Data { get; set; } = default!;
    public DateTime AttachedAt { get; set; } = DateTime.UtcNow;
}
