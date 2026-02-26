using Pandora.Core.Features.Export;
using System.Text.Json;

namespace Pandora.Tests.Features.Export;

public sealed class ExportServiceTests
{
    [Test]
    public void ToJson_SerializesCollection()
    {
        var options = new ExportOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = false
            }
        };

        var service = new ExportService(options);
        var data = new[] { new Sample("alpha", 1) };

        var json = service.ToJson(data);

        Assert.That(json, Does.Contain("\"name\":\"alpha\""));
        Assert.That(json, Does.Contain("\"value\":1"));
    }

    [Test]
    public void ToCsv_WritesHeaderAndRows()
    {
        var options = new ExportOptions
        {
            Delimiter = ";",
            IncludeHeader = true
        };

        var service = new ExportService(options);
        var data = new[]
        {
            new Sample("alpha;beta", 2),
            new Sample("gamma", 3)
        };

        var csv = service.ToCsv(data);
        var lines = csv.TrimEnd('\r', '\n').Split('\n');

        Assert.That(lines.Length, Is.EqualTo(3));
        Assert.That(lines[0], Is.EqualTo("Name;Value"));
        Assert.That(lines[1], Is.EqualTo("\"alpha;beta\";2"));
        Assert.That(lines[2], Is.EqualTo("gamma;3"));
    }

    private sealed record Sample(string Name, int Value);
}
