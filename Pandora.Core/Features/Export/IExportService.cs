using System.Text.Json;

namespace Pandora.Core.Features.Export;

public interface IExportService
{
    string ToJson<T>(IEnumerable<T> data, JsonSerializerOptions? options = null);

    string ToCsv<T>(IEnumerable<T> data, string? delimiter = null, bool includeHeader = true);
}
