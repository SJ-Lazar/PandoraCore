using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Pandora.Core.Features.Export;

public sealed class ExportService : IExportService
{
    private readonly ExportOptions _options;

    public ExportService(ExportOptions options)
    {
        _options = options;
    }

    public string ToJson<T>(IEnumerable<T> data, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var serializerOptions = options ?? _options.JsonSerializerOptions;
        return JsonSerializer.Serialize(data, serializerOptions);
    }

    public string ToCsv<T>(IEnumerable<T> data, string? delimiter = null, bool includeHeader = true)
    {
        ArgumentNullException.ThrowIfNull(data);

        var separator = string.IsNullOrEmpty(delimiter) ? _options.Delimiter : delimiter;
        includeHeader = includeHeader && _options.IncludeHeader;

        var items = data.ToArray();
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToArray();

        var builder = new StringBuilder();

        if (includeHeader)
        {
            builder.AppendLine(string.Join(separator, properties.Select(p => Escape(p.Name, separator))));
        }

        foreach (var item in items)
        {
            var values = properties.Select(p => FormatValue(p.GetValue(item), separator));
            builder.AppendLine(string.Join(separator, values));
        }

        return builder.ToString();
    }

    private static string FormatValue(object? value, string delimiter)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is IFormattable formattable)
        {
            return Escape(formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture), delimiter);
        }

        return Escape(value.ToString() ?? string.Empty, delimiter);
    }

    private static string Escape(string input, string delimiter)
    {
        if (input.IndexOfAny(new[] { '\"', '\n', '\r' }) >= 0 || input.Contains(delimiter, StringComparison.Ordinal))
        {
            return $"\"{input.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return input;
    }
}
