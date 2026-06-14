using ExperimentalPrototype.Evaluation;
using ExperimentalPrototype.Mutation;
using ExperimentalPrototype.Strategies.Coverage;
using ExperimentalPrototype.TestModel;
using ExperimentalPrototype.Tests.Helpers;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.Evaluation;

[TestFixture]
public class MetricsCalculatorTests
{
    private MetricsCalculator _metrics = null!;

    [SetUp]
    public void SetUp() => _metrics = new MetricsCalculator();

    // ── StateCoverage ─────────────────────────────────────────────────────────

    [Test]
    public void StateCoverage_FullCoverageSuite_ReturnsOne()
    {
        var fsm = FsmFixtures.Linear3();
        var suite = FsmFixtures.PathsToSuite("s", new StateCoverageStrategy().GeneratePaths(fsm));

        Assert.That(_metrics.ComputeStateCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void StateCoverage_EmptySuite_ReturnsZero()
    {
        var fsm = FsmFixtures.Linear3();
        Assert.That(_metrics.ComputeStateCoverage(new TestSuite("empty"), fsm), Is.EqualTo(0.0));
    }

    [Test]
    public void StateCoverage_PartialCoverage_ReturnsFractionBetweenZeroAndOne()
    {
        var fsm = FsmFixtures.Linear3();
        // One-step path [connect] visits A and B only → 2/3 state coverage
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions.Take(1));
        var suite = FsmFixtures.MakeSuite("s", tc);

        var cov = _metrics.ComputeStateCoverage(suite, fsm);
        Assert.That(cov, Is.GreaterThan(0.0).And.LessThan(1.0));
    }

    // ── TransitionCoverage ────────────────────────────────────────────────────

    [Test]
    public void TransitionCoverage_FullCoverageSuite_ReturnsOne()
    {
        var fsm = FsmFixtures.Branching4();
        var suite = FsmFixtures.PathsToSuite("s", new TransitionCoverageStrategy().GeneratePaths(fsm));

        Assert.That(_metrics.ComputeTransitionCoverage(suite, fsm), Is.EqualTo(1.0).Within(1e-5));
    }

    [Test]
    public void TransitionCoverage_EmptySuite_ReturnsZero()
    {
        var fsm = FsmFixtures.Linear3();
        Assert.That(_metrics.ComputeTransitionCoverage(new TestSuite("empty"), fsm), Is.EqualTo(0.0));
    }

    // ── APFD ──────────────────────────────────────────────────────────────────

    [Test]
    public void APFD_EmptySuite_ReturnsZero()
    {
        // No mutants, no expected outputs — nothing to kill
        Assert.That(_metrics.ComputeAPFD(
            new TestSuite("empty"),
            new List<Mutant>(),
            new Dictionary<string, List<string>>()), Is.EqualTo(0.0));
    }

    [Test]
    public void APFD_NoMutants_ReturnsZero()
    {
        var fsm = FsmFixtures.Toggle2();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);
        var suite = FsmFixtures.MakeSuite("s", tc);

        Assert.That(_metrics.ComputeAPFD(
            suite,
            new List<Mutant>(),
            new Dictionary<string, List<string>> { [tc.Id] = new() { "x", "y" } }),
            Is.EqualTo(0.0));
    }

    [Test]
    public void APFD_SingleCaseKillsMutantAtPositionOne_ReturnsHalf()
    {
        // Toggle2: S0 -[a/x]-> S1 -[b/y]-> S0
        // TC = [a, b], expected outputs = ["x", "y"]
        // Mutant changes output of "a" from "x" to "y" → TC kills it at position 1
        // APFD = 1 - TF/(n*m) + 1/(2n) = 1 - 1/(1*1) + 1/2 = 0.5
        var fsm = FsmFixtures.Toggle2();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions); // [a, b]
        var suite = FsmFixtures.MakeSuite("s", tc);

        var expectedOutputs = new Dictionary<string, List<string>>
        {
            [tc.Id] = new List<string> { "x", "y" }
        };

        // OutputMutation on Toggle2 changes "x" → "y" on the first transition
        var mutant = new OutputMutation().Apply(fsm).First();

        var apfd = _metrics.ComputeAPFD(suite, new List<Mutant> { mutant }, expectedOutputs);

        Assert.That(apfd, Is.EqualTo(0.5).Within(1e-10));
    }
}
