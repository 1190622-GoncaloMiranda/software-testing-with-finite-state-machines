using ExperimentalPrototype.Evaluation;
using ExperimentalPrototype.Strategies.Coverage;
using ExperimentalPrototype.Tests.Helpers;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.Strategies;

[TestFixture]
public class CoverageStrategyTests
{
    private MetricsCalculator _metrics = null!;

    [SetUp]
    public void SetUp() => _metrics = new MetricsCalculator();

    // ── StateCoverageStrategy ─────────────────────────────────────────────────

    [Test]
    public void StateCoverage_GeneratedSuite_AchievesFullStateCoverage()
    {
        var fsm = FsmFixtures.Linear3();
        var suite = FsmFixtures.PathsToSuite("s", new StateCoverageStrategy().GeneratePaths(fsm));

        Assert.That(_metrics.ComputeStateCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void StateCoverage_NonTrivialFSM_ProducesAtLeastOnePath()
    {
        var paths = new StateCoverageStrategy().GeneratePaths(FsmFixtures.Branching4());
        Assert.That(paths, Is.Not.Empty);
    }

    // ── TransitionCoverageStrategy ────────────────────────────────────────────

    [Test]
    public void TransitionCoverage_GeneratedSuite_AchievesFullTransitionCoverage()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        Assert.That(_metrics.ComputeTransitionCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void TransitionCoverage_SuiteCoversAtLeastAsMuchAsStateCoverage()
    {
        var fsm = FsmFixtures.Branching4();
        var tcSuite = FsmFixtures.PathsToSuite("tc", new TransitionCoverageStrategy().GeneratePaths(fsm));
        var scSuite = FsmFixtures.PathsToSuite("sc", new StateCoverageStrategy().GeneratePaths(fsm));

        Assert.That(
            _metrics.ComputeTransitionCoverage(tcSuite, fsm),
            Is.GreaterThanOrEqualTo(_metrics.ComputeTransitionCoverage(scSuite, fsm)));
    }

    // ── TransitionPairCoverageStrategy ────────────────────────────────────────

    [Test]
    public void TransitionPairCoverage_GeneratedSuite_CoversAllIndividualTransitions()
    {
        var fsm = FsmFixtures.Linear3();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionPairCoverageStrategy().GeneratePaths(fsm));

        // Pair coverage is a strict superset of transition coverage
        Assert.That(_metrics.ComputeTransitionCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void TransitionPairCoverage_NonTrivialFSM_ProducesAtLeastOnePath()
    {
        var paths = new TransitionPairCoverageStrategy().GeneratePaths(FsmFixtures.Branching4());
        Assert.That(paths, Is.Not.Empty);
    }

    // ── SequenceCoverageStrategy ──────────────────────────────────────────────

    [Test]
    public void SequenceCoverage_AllPaths_HaveLengthAtMostMaxLength()
    {
        var fsm = FsmFixtures.Linear3();
        var paths = new SequenceCoverageStrategy(maxLength: 2).GeneratePaths(fsm);

        Assert.That(paths, Is.Not.Empty);
        Assert.That(paths, Has.All.Matches<List<ExperimentalPrototype.Core.Transition>>(p => p.Count <= 2));
    }

    [Test]
    public void SequenceCoverage_MaxLengthOne_ProducesOnlySingleTransitionPaths()
    {
        var fsm = FsmFixtures.Branching4();
        var paths = new SequenceCoverageStrategy(maxLength: 1).GeneratePaths(fsm);

        Assert.That(paths, Is.Not.Empty);
        Assert.That(paths, Has.All.Matches<List<ExperimentalPrototype.Core.Transition>>(p => p.Count == 1));
    }
}
