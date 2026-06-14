using FinalArtefact.Engine;
using FinalArtefact.Strategies.Coverage;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Strategies;

[TestFixture]
public class CoverageStrategyTests
{
    private static double TransitionCoverage(FinalArtefact.TestModel.TestSuite suite, FinalArtefact.Core.FSM fsm)
    {
        if (fsm.Transitions.Count == 0) return 1.0;
        var covered = suite.GetCoveredTransitionIds();
        int hit = fsm.Transitions.Count(t => covered.Contains(t.Id));
        return (double)hit / fsm.Transitions.Count;
    }

    private static double StateCoverage(FinalArtefact.TestModel.TestSuite suite, FinalArtefact.Core.FSM fsm)
    {
        var reachable = fsm.GetReachableStates().Select(s => s.Name).ToHashSet();
        if (reachable.Count == 0) return 1.0;
        var visited = suite.GetVisitedStateNames();
        int covered = visited.Count(s => reachable.Contains(s));
        return (double)covered / reachable.Count;
    }

    // ── StateCoverageStrategy ─────────────────────────────────────────────────

    [Test]
    public void StateCoverage_GeneratedSuite_AchievesFullStateCoverage()
    {
        var fsm = FsmFixtures.Linear3();
        var suite = FsmFixtures.PathsToSuite("s", new StateCoverageStrategy().GeneratePaths(fsm));

        Assert.That(StateCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
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

        Assert.That(TransitionCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void TransitionCoverage_SuiteCoversAtLeastAsMuchAsStateCoverage()
    {
        var fsm = FsmFixtures.Branching4();
        var tcSuite = FsmFixtures.PathsToSuite("tc", new TransitionCoverageStrategy().GeneratePaths(fsm));
        var scSuite = FsmFixtures.PathsToSuite("sc", new StateCoverageStrategy().GeneratePaths(fsm));

        Assert.That(
            TransitionCoverage(tcSuite, fsm),
            Is.GreaterThanOrEqualTo(TransitionCoverage(scSuite, fsm)));
    }

    // ── TransitionPairCoverageStrategy ────────────────────────────────────────

    [Test]
    public void TransitionPairCoverage_GeneratedSuite_CoversAllIndividualTransitions()
    {
        var fsm = FsmFixtures.Linear3();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionPairCoverageStrategy().GeneratePaths(fsm));

        Assert.That(TransitionCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
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
        Assert.That(paths, Has.All.Matches<List<FinalArtefact.Core.Transition>>(p => p.Count <= 2));
    }

    [Test]
    public void SequenceCoverage_MaxLengthOne_ProducesOnlySingleTransitionPaths()
    {
        var fsm = FsmFixtures.Branching4();
        var paths = new SequenceCoverageStrategy(maxLength: 1).GeneratePaths(fsm);

        Assert.That(paths, Is.Not.Empty);
        Assert.That(paths, Has.All.Matches<List<FinalArtefact.Core.Transition>>(p => p.Count == 1));
    }
}
