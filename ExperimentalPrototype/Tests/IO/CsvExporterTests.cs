using ExperimentalPrototype.Evaluation;
using ExperimentalPrototype.IO;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.IO;

[TestFixture]
public class CsvExporterTests
{
    private CsvExporter _exporter = null!;
    private string _tempPath = null!;

    [SetUp]
    public void SetUp()
    {
        _exporter = new CsvExporter();
        _tempPath = Path.Combine(Path.GetTempPath(), $"fsm_test_{Guid.NewGuid()}.csv");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Test]
    public void Export_WritesHeaderAsFirstLine()
    {
        _exporter.Export([], _tempPath);

        var firstLine = File.ReadLines(_tempPath).First();
        Assert.That(firstLine, Does.StartWith("FSM,"));
        Assert.That(firstLine, Does.Contain("ReductionRatio"));
        Assert.That(firstLine, Does.Contain("ExecutionTimeSavings"));
    }

    [Test]
    public void Export_WritesOneRowPerResult()
    {
        var results = new[]
        {
            new EvaluationResult { SuiteName = "FSM|SC|DR|CB" },
            new EvaluationResult { SuiteName = "FSM|TC|DR|CB" }
        };

        _exporter.Export(results, _tempPath);

        var lines = File.ReadAllLines(_tempPath);
        Assert.That(lines, Has.Length.EqualTo(3)); // header + 2 data rows
    }

    [Test]
    public void Export_NaNReductionRatio_WritesEmptyCell()
    {
        // PreReductionSuiteSize = 0 → ReductionRatio = NaN → empty CSV cell
        var result = new EvaluationResult
        {
            SuiteName = "F|SC|DR|CB",
            SuiteSize = 0,
            PreReductionSuiteSize = 0
        };

        _exporter.Export(new[] { result }, _tempPath);

        var dataLine = File.ReadAllLines(_tempPath)[1];
        Assert.That(dataLine, Does.Contain(",,"));
    }

    [Test]
    public void Append_CreatesFileWithHeaderWhenFileMissing()
    {
        _exporter.Append(new[] { new EvaluationResult { SuiteName = "F|SC|DR|CB" } }, _tempPath);

        var lines = File.ReadAllLines(_tempPath);
        Assert.That(lines.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(lines[0], Does.StartWith("FSM,"));
    }

    [Test]
    public void Append_SecondCall_DoesNotDuplicateHeader()
    {
        var result = new EvaluationResult { SuiteName = "F|SC|DR|CB" };

        _exporter.Append(new[] { result }, _tempPath);
        _exporter.Append(new[] { result }, _tempPath);

        var lines = File.ReadAllLines(_tempPath);
        // header (1) + 2 data rows = 3 lines total
        Assert.That(lines, Has.Length.EqualTo(3));
    }
}
