using System.Text.Json;

namespace Pandora.Core.Features.Export;

public sealed class ExportOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string Delimiter { get; set; } = ",";

    public bool IncludeHeader { get; set; } = true;
}
