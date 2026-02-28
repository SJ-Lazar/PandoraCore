namespace Pandora.Core.Features.WorkItem;

public enum MetaDataAttributeType
{
    Text,
    Dropdown,
    CheckBox,
    RadioButton,
    FileAttachment,
    Number,
    Currency,
    Date,
    DateTime,
    Summary
}

public class MetaDataAttribute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public MetaDataAttributeType Type { get; set; }
    public object? Value { get; set; }
    public List<string>? Options { get; set; } // For dropdown/radio
    public List<MetaDataAttributeRule> Rules { get; set; } = new();
}

public class LineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<MetaDataAttribute> Attributes { get; set; } = new();
}
