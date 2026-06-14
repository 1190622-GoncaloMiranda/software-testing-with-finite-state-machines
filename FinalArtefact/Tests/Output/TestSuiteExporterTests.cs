using System.Text.Json;
using FinalArtefact.Output;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Output;

[TestFixture]
public class TestSuiteExporterTests
{
    private string _tempDir = null!;
    private TestSuiteExporter _exporter = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _exporter = new TestSuiteExporter();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string OutputPath => Path.Combine(_tempDir, "test_suite.json");

    private void Export(string suiteName = "suite")
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite(suiteName,
            new FinalArtefact.Strategies.Coverage.TransitionCoverageStrategy().GeneratePaths(fsm));

        _exporter.Export(suite, fsm.Name, "TransitionCoverage", "DuplicateRemoval", "CoverageBased", OutputPath);
    }

    [Test]
    public void Export_CreatesJsonFile()
    {
        Export();

        Assert.That(File.Exists(OutputPath), Is.True);
    }

    [Test]
    public void Export_JsonContainsTestCasesArray()
    {
        Export();

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        Assert.That(doc.RootElement.TryGetProperty("testCases", out _), Is.True);
    }

    [Test]
    public void Export_TestCasesHaveSequentialPriority()
    {
        Export();

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        var cases = doc.RootElement.GetProperty("testCases").EnumerateArray().ToList();

        Assert.That(cases, Is.Not.Empty);
        for (int i = 0; i < cases.Count; i++)
            Assert.That(cases[i].GetProperty("priority").GetInt32(), Is.EqualTo(i + 1));
    }

    [Test]
    public void Export_EachStepHasRequiredFields()
    {
        Export();

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        var cases = doc.RootElement.GetProperty("testCases").EnumerateArray();

        foreach (var tc in cases)
        {
            foreach (var step in tc.GetProperty("steps").EnumerateArray())
            {
                Assert.That(step.TryGetProperty("fromState",      out _), Is.True);
                Assert.That(step.TryGetProperty("input",          out _), Is.True);
                Assert.That(step.TryGetProperty("expectedOutput", out _), Is.True);
                Assert.That(step.TryGetProperty("toState",        out _), Is.True);
            }
        }
    }

    [Test]
    public void Export_MetadataContainsStrategyNames()
    {
        Export();

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        var meta = doc.RootElement.GetProperty("metadata");

        Assert.That(meta.GetProperty("coverageStrategy").GetString(),       Is.EqualTo("TransitionCoverage"));
        Assert.That(meta.GetProperty("reductionStrategy").GetString(),      Is.EqualTo("DuplicateRemoval"));
        Assert.That(meta.GetProperty("prioritisationStrategy").GetString(), Is.EqualTo("CoverageBased"));
    }

    [Test]
    public void Export_MetadataTotalTestCasesMatchesActual()
    {
        Export();

        var doc = JsonDocument.Parse(File.ReadAllText(OutputPath));
        int reported = doc.RootElement.GetProperty("metadata").GetProperty("totalTestCases").GetInt32();
        int actual   = doc.RootElement.GetProperty("testCases").EnumerateArray().Count();

        Assert.That(reported, Is.EqualTo(actual));
    }

    [Test]
    public void Export_CreatesOutputDirectoryIfMissing()
    {
        var deepPath = Path.Combine(_tempDir, "a", "b", "c", "test_suite.json");

        var fsm = FsmFixtures.Toggle2();
        var suite = FsmFixtures.PathsToSuite("s",
            new FinalArtefact.Strategies.Coverage.TransitionCoverageStrategy().GeneratePaths(fsm));

        _exporter.Export(suite, fsm.Name, "TC", "DR", "CB", deepPath);

        Assert.That(File.Exists(deepPath), Is.True);
    }
}
