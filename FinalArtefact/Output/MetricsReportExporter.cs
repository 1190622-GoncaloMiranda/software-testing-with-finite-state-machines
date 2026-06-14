using System.Text.Json;

namespace FinalArtefact.Output;

public class MetricsReportExporter
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Export(GenerationReport report, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, _options));
        Console.WriteLine($"[MetricsReportExporter] Metrics written to: {outputPath}");
    }
}
