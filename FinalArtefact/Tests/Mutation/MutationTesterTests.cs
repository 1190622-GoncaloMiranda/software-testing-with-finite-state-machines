using FinalArtefact.Mutation;
using FinalArtefact.Strategies.Coverage;
using FinalArtefact.TestModel;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Mutation;

[TestFixture]
public class MutationTesterTests
{
    private MutationTester _tester = null!;
    private MutationEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _tester = new MutationTester();
        _engine = new MutationEngine();
    }

    [Test]
    public void Run_EmptySuite_KillsNoMutants()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutants = _engine.GenerateMutants(fsm);

        var result = _tester.Run(new TestSuite("empty"), fsm, mutants);

        Assert.That(result.KilledMutants, Is.EqualTo(0));
    }

    [Test]
    public void Run_FullCoverageSuite_KillsAtLeastOneMutant()
    {
        var fsm = FsmFixtures.Linear3();
        var mutants = _engine.GenerateMutants(fsm);
        var suite = FsmFixtures.PathsToSuite("s",
            new TransitionCoverageStrategy().GeneratePaths(fsm));

        var result = _tester.Run(suite, fsm, mutants);

        Assert.That(result.KilledMutants, Is.GreaterThan(0));
    }

    [Test]
    public void Run_MutationScore_IsRatioOfKilledToTotal()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutants = _engine.GenerateMutants(fsm);
        var suite = FsmFixtures.PathsToSuite("s",
            new TransitionCoverageStrategy().GeneratePaths(fsm));

        var result = _tester.Run(suite, fsm, mutants);

        var expected = result.TotalMutants == 0
            ? 1.0
            : (double)result.KilledMutants / result.TotalMutants;
        Assert.That(result.MutationScore, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void Run_ScoreByOperator_TotalCountsMatchMutantList()
    {
        var fsm = FsmFixtures.Linear3();
        var mutants = _engine.GenerateMutants(fsm);
        var suite = FsmFixtures.PathsToSuite("s",
            new TransitionCoverageStrategy().GeneratePaths(fsm));

        var result = _tester.Run(suite, fsm, mutants);
        var byOp = result.ScoreByOperator();

        Assert.That(byOp.Values.Sum(v => v.Total), Is.EqualTo(mutants.Count));
        Assert.That(byOp, Has.All.Matches<KeyValuePair<string, (int Killed, int Total)>>(
            kv => kv.Value.Killed <= kv.Value.Total));
    }

    [Test]
    public void Run_AfterResettingIsKilled_EmptySuiteKillsNothing()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutants = _engine.GenerateMutants(fsm);
        var suite = FsmFixtures.PathsToSuite("s",
            new TransitionCoverageStrategy().GeneratePaths(fsm));

        _tester.Run(suite, fsm, mutants);

        foreach (var m in mutants) m.IsKilled = false;

        _tester.Run(new TestSuite("empty"), fsm, mutants);

        Assert.That(mutants.Count(m => m.IsKilled), Is.EqualTo(0));
    }
}
