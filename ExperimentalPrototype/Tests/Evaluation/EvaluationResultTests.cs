using ExperimentalPrototype.Evaluation;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.Evaluation;

[TestFixture]
public class EvaluationResultTests
{
    // ── ReductionRatio ────────────────────────────────────────────────────────

    [Test]
    public void ReductionRatio_NoReduction_ReturnsZero()
    {
        var r = new EvaluationResult { SuiteSize = 10, PreReductionSuiteSize = 10 };
        Assert.That(r.ReductionRatio, Is.EqualTo(0.0).Within(1e-10));
    }

    [Test]
    public void ReductionRatio_HalfRemoved_ReturnsPointFive()
    {
        var r = new EvaluationResult { SuiteSize = 5, PreReductionSuiteSize = 10 };
        Assert.That(r.ReductionRatio, Is.EqualTo(0.5).Within(1e-10));
    }

    [Test]
    public void ReductionRatio_AllRemoved_ReturnsOne()
    {
        var r = new EvaluationResult { SuiteSize = 0, PreReductionSuiteSize = 8 };
        Assert.That(r.ReductionRatio, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void ReductionRatio_ZeroPreSize_ReturnsNaN()
    {
        var r = new EvaluationResult { SuiteSize = 0, PreReductionSuiteSize = 0 };
        Assert.That(r.ReductionRatio, Is.NaN);
    }

    // ── ExecutionTimeSavings ──────────────────────────────────────────────────

    [Test]
    public void ExecutionTimeSavings_NoReduction_ReturnsZero()
    {
        var r = new EvaluationResult { TotalSteps = 100, PreReductionTotalSteps = 100 };
        Assert.That(r.ExecutionTimeSavings, Is.EqualTo(0.0).Within(1e-10));
    }

    [Test]
    public void ExecutionTimeSavings_SixtyPercentSaved_ReturnsPointSix()
    {
        var r = new EvaluationResult { TotalSteps = 40, PreReductionTotalSteps = 100 };
        Assert.That(r.ExecutionTimeSavings, Is.EqualTo(0.6).Within(1e-10));
    }

    [Test]
    public void ExecutionTimeSavings_ZeroPreSteps_ReturnsNaN()
    {
        var r = new EvaluationResult { TotalSteps = 0, PreReductionTotalSteps = 0 };
        Assert.That(r.ExecutionTimeSavings, Is.NaN);
    }

    // ── Strategy name parsing ─────────────────────────────────────────────────

    [Test]
    public void EvaluationResult_ParsesAllStrategyNamesFromPipeSeparatedSuiteName()
    {
        var r = new EvaluationResult
        {
            SuiteName = "MyFSM|StateCoverage|DuplicateRemoval|CoverageBasedPrioritization"
        };

        Assert.That(r.FSMName,                  Is.EqualTo("MyFSM"));
        Assert.That(r.CoverageStrategy,         Is.EqualTo("StateCoverage"));
        Assert.That(r.ReductionStrategy,        Is.EqualTo("DuplicateRemoval"));
        Assert.That(r.PrioritizationStrategy,   Is.EqualTo("CoverageBasedPrioritization"));
    }
}
