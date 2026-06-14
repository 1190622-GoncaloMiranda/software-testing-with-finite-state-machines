using FinalArtefact.Strategies.Coverage;
using FinalArtefact.Strategies.Prioritization;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Strategies;

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

        Assert.That(prioritised.TestCases.Select(tc => tc.Id), Is.EquivalentTo(originalIds));
    }

    [Test]
    public void CoverageBased_EmptySuite_ReturnsEmptySuite()
    {
        var fsm = FsmFixtures.Toggle2();
        var empty = FsmFixtures.MakeSuite("s");

        var prioritised = new CoverageBasedPrioritization().Prioritize(empty, fsm);

        Assert.That(prioritised.Size, Is.EqualTo(0));
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

        Assert.That(prioritised.TestCases.Select(tc => tc.Id), Is.EquivalentTo(originalIds));
    }

    [Test]
    public void WeightedTransition_EmptySuite_ReturnsEmptySuite()
    {
        var fsm = FsmFixtures.Toggle2();
        var empty = FsmFixtures.MakeSuite("s");

        var prioritised = new WeightedTransitionPrioritization().Prioritize(empty, fsm);

        Assert.That(prioritised.Size, Is.EqualTo(0));
    }
}
