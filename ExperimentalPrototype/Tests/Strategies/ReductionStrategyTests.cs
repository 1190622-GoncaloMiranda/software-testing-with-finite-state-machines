using ExperimentalPrototype.Evaluation;
using ExperimentalPrototype.Strategies.Coverage;
using ExperimentalPrototype.Strategies.Reduction;
using ExperimentalPrototype.Tests.Helpers;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.Strategies;

[TestFixture]
public class ReductionStrategyTests
{
    private MetricsCalculator _metrics = null!;

    [SetUp]
    public void SetUp() => _metrics = new MetricsCalculator();

    // ── DuplicateRemovalStrategy ──────────────────────────────────────────────

    [Test]
    public void DuplicateRemoval_SuiteWithNoDuplicates_ReturnsSameSize()
    {
        var fsm = FsmFixtures.Linear3();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var reduced = new DuplicateRemovalStrategy().Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.EqualTo(suite.Size));
    }

    [Test]
    public void DuplicateRemoval_SuiteWithExactDuplicates_ReducesSize()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions.Take(1));
        var suite = FsmFixtures.MakeSuite("s", tc, tc.Clone(), tc.Clone());

        var reduced = new DuplicateRemovalStrategy().Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.LessThan(suite.Size));
    }

    // ── CoveragePreservingReductionStrategy ───────────────────────────────────

    [Test]
    public void CoveragePreserving_ReducedSuite_PreservesFullTransitionCoverage()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var reduced = new CoveragePreservingReductionStrategy().Reduce(suite, fsm);

        Assert.That(_metrics.ComputeTransitionCoverage(reduced, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void CoveragePreserving_OutputSize_IsAtMostInputSize()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var reduced = new CoveragePreservingReductionStrategy().Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.LessThanOrEqualTo(suite.Size));
    }

    // ── SimilarityReductionStrategy ───────────────────────────────────────────

    [Test]
    public void SimilarityReduction_IdenticalTestCases_ReducesToFewerCases()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);
        var suite = FsmFixtures.MakeSuite("s", tc, tc.Clone(), tc.Clone());

        var reduced = new SimilarityReductionStrategy(similarityThreshold: 0.5).Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.LessThan(suite.Size));
    }

    [Test]
    public void SimilarityReduction_OutputSize_IsAtMostInputSize()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var reduced = new SimilarityReductionStrategy(similarityThreshold: 0.8).Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.LessThanOrEqualTo(suite.Size));
    }

    // ── MutationScorePreservingReductionStrategy ─────────────────────────────────────────────────

    [Test]
    public void MutationScorePreserving_AlwaysRetainsAtLeastOneTestCase()
    {
        var fsm = FsmFixtures.Toggle2();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var reduced = new MutationScorePreservingReductionStrategy().Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void MutationScorePreserving_OutputSize_IsAtMostInputSize()
    {
        var fsm = FsmFixtures.Toggle2();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        var reduced = new MutationScorePreservingReductionStrategy().Reduce(suite, fsm);

        Assert.That(reduced.Size, Is.LessThanOrEqualTo(suite.Size));
    }
}
