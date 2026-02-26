using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Pandora.Core.Features.Import;

public sealed class Import
{
    private readonly JsonSerializerOptions _jsonOptions;

    public Import(JsonSerializerOptions? jsonOptions = null)
    {
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public IReadOnlyList<T> FromJson<T>(string json, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var serializerOptions = options ?? _jsonOptions;
        var result = JsonSerializer.Deserialize<IEnumerable<T>>(json, serializerOptions);

        return result?.ToArray() ?? Array.Empty<T>();
    }

    public IReadOnlyList<T> FromCsv<T>(string csv, string? delimiter = null, bool hasHeader = true) where T : new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csv);

        var separator = string.IsNullOrEmpty(delimiter) ? "," : delimiter;
        var rows = ParseCsv(csv, separator[0]);

        if (rows.Count == 0)
        {
            return Array.Empty<T>();
        }

        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite)
            .ToArray();

        var header = hasHeader
            ? rows.First()
            : properties.Select(p => p.Name).ToArray();

        var map = header
            .Select(name => properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var startIndex = hasHeader ? 1 : 0;
        var items = new List<T>(Math.Max(0, rows.Count - startIndex));

        for (var i = startIndex; i < rows.Count; i++)
        {
            var row = rows[i];
            var instance = new T();

            var length = Math.Min(row.Length, map.Length);
            for (var j = 0; j < length; j++)
            {
                var property = map[j];
                if (property is null)
                {
                    continue;
                }

                var value = ConvertValue(row[j], property.PropertyType);
                property.SetValue(instance, value);
            }

            items.Add(instance);
        }

        return items;
    }

    private static List<string[]> ParseCsv(string csv, char delimiter)
    {
        var rows = new List<string[]>();
        var currentField = new StringBuilder();
        var currentRow = new List<string>();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == delimiter && !inQuotes)
            {
                currentRow.Add(currentField.ToString());
                currentField.Clear();
                continue;
            }

            if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                {
                    i++;
                }

                currentRow.Add(currentField.ToString());
                currentField.Clear();

                if (currentRow.Count > 0)
                {
                    rows.Add(currentRow.ToArray());
                }

                currentRow.Clear();
                continue;
            }

            currentField.Append(c);
        }

        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString());
            rows.Add(currentRow.ToArray());
        }

        return rows;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(trimmed))
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null)
            {
                return null;
            }

            return targetType == typeof(string) ? string.Empty : Activator.CreateInstance(targetType);
        }

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType == typeof(string))
        {
            return trimmed;
        }

        if (actualType.IsEnum)
        {
            return Enum.Parse(actualType, trimmed, ignoreCase: true);
        }

        if (actualType == typeof(Guid))
        {
            return Guid.Parse(trimmed);
        }

        if (actualType == typeof(DateTime))
        {
            return DateTime.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (actualType == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (actualType == typeof(TimeSpan))
        {
            return TimeSpan.Parse(trimmed, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(trimmed, actualType, CultureInfo.InvariantCulture);
    }
}
