namespace Pandora.Core.Features.WorkItem;

public enum MetaDataAttributeRuleType
{
    Visibility,
    Required,
    ValueConstraint,
    Tooltip,
    Color,
    Custom
}

public class MetaDataAttributeRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MetaDataAttributeRuleType RuleType { get; set; }
    public string Expression { get; set; } = string.Empty; // e.g., "Value == 'Yes'"
    public Guid? TargetAttributeId { get; set; } // For cross-attribute rules

    // For Tooltip rule
    public string? TooltipText { get; set; }

    // For Color rule
    public string? ColorValue { get; set; } // e.g., hex or named color
}
