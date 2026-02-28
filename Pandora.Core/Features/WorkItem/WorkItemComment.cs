namespace Pandora.Core.Features.WorkItem;

public class WorkItemComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Author { get; set; }
}
