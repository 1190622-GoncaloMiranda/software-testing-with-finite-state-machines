using ExperimentalPrototype.Strategies.Coverage;
using ExperimentalPrototype.Strategies.Prioritization;
using ExperimentalPrototype.Tests.Helpers;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.Strategies;

[TestFixture]
public class PrioritizationStrategyTests
{
    // ── CoverageBasedPrioritization ───────────────────────────────────────────

    [Test]
    public void CoverageBased_OutputHasSameSizeAsInput()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var prioritised = new CoverageBasedPrioritization().Prioritize(suite, fsm);

        Assert.That(prioritised.Size, Is.EqualTo(suite.Size));
    }

    [Test]
    public void CoverageBased_OutputContainsAllOriginalTestCaseIds()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));
        var originalIds = suite.TestCases.Select(tc => tc.Id).ToHashSet();

        var prioritised = new CoverageBasedPrioritization().Prioritize(suite, fsm);

        Assert.That(prioritised.TestCases.Select(tc => tc.Id),
            Is.EquivalentTo(originalIds));
    }

    // ── WeightedTransitionPrioritization ─────────────────────────────────────

    [Test]
    public void WeightedTransition_OutputHasSameSizeAsInput()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var prioritised = new WeightedTransitionPrioritization().Prioritize(suite, fsm);

        Assert.That(prioritised.Size, Is.EqualTo(suite.Size));
    }

    [Test]
    public void WeightedTransition_OutputContainsAllOriginalTestCaseIds()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));
        var originalIds = suite.TestCases.Select(tc => tc.Id).ToHashSet();

        var prioritised = new WeightedTransitionPrioritization().Prioritize(suite, fsm);

        Assert.That(prioritised.TestCases.Select(tc => tc.Id),
            Is.EquivalentTo(originalIds));
    }
}
