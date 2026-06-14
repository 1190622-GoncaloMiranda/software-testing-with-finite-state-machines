using System.Text.Json;
using FinalArtefact.Output;
using NUnit.Framework;

namespace FinalArtefact.Tests.Output;

[TestFixture]
public class MetricsReportExporterTests
{
    private string _tempDir = null!;
    private MetricsReportExporter _exporter = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _exporter = new MetricsReportExporter();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string OutputPath => Path.Combine(_tempDir, "metrics_report.json");

    private GenerationReport BuildReport() => new()
    {
        FsmName     = "TestFSM",
        GeneratedAt = "2026-01-01T00:00:00Z",
        Configuration = new ReportConfiguration
        {
            CoverageStrategy       = "TransitionCoverage",
            ReductionStrategy      = "MutationScorePreserving",
            PrioritisationStrategy = "CoverageBased"
        },
        SuiteMetrics = new SuiteMetrics
        {
            SuiteSize              = 5,
            TotalSteps             = 20,
            PreReductionSuiteSize  = 10,
            PreReductionTotalSteps = 40,
            ReductionRatio         = 0.5,
            ExecutionTimeSavings   = 0.5,
            StateCoverage          = 1.0,
            TransitionCoverage     = 1.0,
            Apfd                   = 0.85,
            GenerationTimeMs       = 42.0
        },
        MutationMetrics = new MutationMetrics
        {
            PreReductionMutationScore  = 0.9,
            PostReductionMutationScore = 0.9,
            AdjustedMutationScore      = 1.0,
            TotalMutants               = 100,
            KilledMutants              = 90,
            StrategyUnreachableMutants = 10,
            SurvivingMutants           = 0,
            PerOperatorScores          = new Dictionary<string, double?> { ["OutputMutation"] = 0.9 }
        },
        Warnings = new List<string> { "Test warning" }
    };

    [Test]
    public void Export_CreatesJsonFile()
    {
        _exporter.Export(BuildReport(), OutputPath);

        Assert.That(File.Exists(OutputPath), Is.True);
    }

    [Test]
    public void Export_FileContainsValidJson()
    {
        _exporter.Export(BuildReport(), OutputPath);

        Assert.DoesNotThrow(() => JsonDocument.Parse(File.ReadAllText(OutputPath)));
    }

    [Test]
    public void Export_FsmNameIsCorrect()
    {
        _exporter.Export(BuildReport(), OutputPath);

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        Assert.That(doc.RootElement.GetProperty("fsmName").GetString(), Is.EqualTo("TestFSM"));
    }

    [Test]
    public void Export_SuiteMetricsArePresent()
    {
        _exporter.Export(BuildReport(), OutputPath);

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        var metrics = doc.RootElement.GetProperty("suiteMetrics");

        Assert.That(metrics.GetProperty("suiteSize").GetInt32(),     Is.EqualTo(5));
        Assert.That(metrics.GetProperty("reductionRatio").GetDouble(), Is.EqualTo(0.5).Within(1e-10));
    }

    [Test]
    public void Export_MutationMetricsArePresent()
    {
        _exporter.Export(BuildReport(), OutputPath);

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        var mutation = doc.RootElement.GetProperty("mutationMetrics");

        Assert.That(mutation.GetProperty("totalMutants").GetInt32(),       Is.EqualTo(100));
        Assert.That(mutation.GetProperty("adjustedMutationScore").GetDouble(), Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void Export_WarningsListIsPresent()
    {
        _exporter.Export(BuildReport(), OutputPath);

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        var warnings = doc.RootElement.GetProperty("warnings").EnumerateArray().ToList();

        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].GetString(), Is.EqualTo("Test warning"));
    }

    [Test]
    public void Export_CreatesOutputDirectoryIfMissing()
    {
        var deepPath = Path.Combine(_tempDir, "nested", "dir", "metrics_report.json");

        _exporter.Export(BuildReport(), deepPath);

        Assert.That(File.Exists(deepPath), Is.True);
    }
}
