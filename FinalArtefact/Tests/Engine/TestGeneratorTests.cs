using FinalArtefact.Core;
using FinalArtefact.Engine;
using FinalArtefact.Strategies;
using FinalArtefact.TestModel;
using FinalArtefact.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace FinalArtefact.Tests.Engine;

[TestFixture]
public class TestGeneratorTests
{
    private ICoverageStrategy _coverage = null!;
    private IReductionStrategy _reduction = null!;
    private IPrioritizationStrategy _prioritization = null!;
    private FSM _fsm = null!;

    [SetUp]
    public void SetUp()
    {
        _fsm = FsmFixtures.Linear3();

        _coverage = Substitute.For<ICoverageStrategy>();
        _coverage.Name.Returns("MockCoverage");

        _reduction = Substitute.For<IReductionStrategy>();
        _reduction.Name.Returns("MockReduction");
        _reduction.Reduce(Arg.Any<TestSuite>(), Arg.Any<FSM>())
            .Returns(callInfo => callInfo.ArgAt<TestSuite>(0));

        _prioritization = Substitute.For<IPrioritizationStrategy>();
        _prioritization.Name.Returns("MockPrioritization");
        _prioritization.Prioritize(Arg.Any<TestSuite>(), Arg.Any<FSM>())
            .Returns(callInfo => callInfo.ArgAt<TestSuite>(0));
    }

    private List<List<Transition>> SinglePath(Transition t) => new() { new List<Transition> { t } };

    [Test]
    public void Generate_CallsCoverageStrategy_ExactlyOnce()
    {
        _coverage.GeneratePaths(Arg.Any<FSM>()).Returns(SinglePath(_fsm.Transitions[0]));

        new TestGenerator(_coverage, _reduction, _prioritization).Generate(_fsm);

        _coverage.Received(1).GeneratePaths(Arg.Any<FSM>());
    }

    [Test]
    public void Generate_CallsReductionStrategy_ExactlyOnce()
    {
        _coverage.GeneratePaths(Arg.Any<FSM>()).Returns(SinglePath(_fsm.Transitions[0]));

        new TestGenerator(_coverage, _reduction, _prioritization).Generate(_fsm);

        _reduction.Received(1).Reduce(Arg.Any<TestSuite>(), Arg.Any<FSM>());
    }

    [Test]
    public void Generate_CallsPrioritizationStrategy_ExactlyOnce()
    {
        _coverage.GeneratePaths(Arg.Any<FSM>()).Returns(SinglePath(_fsm.Transitions[0]));

        new TestGenerator(_coverage, _reduction, _prioritization).Generate(_fsm);

        _prioritization.Received(1).Prioritize(Arg.Any<TestSuite>(), Arg.Any<FSM>());
    }

    [Test]
    public void Generate_PreReductionSize_IsCapturedBeforeReduction()
    {
        var threePaths = Enumerable.Range(0, 3)
            .Select(_ => new List<Transition> { _fsm.Transitions[0] })
            .ToList();
        var singleCase = FsmFixtures.MakeTestCase(new[] { _fsm.Transitions[0] });
        var reducedSuite = new TestSuite("reduced", new[] { singleCase });

        _coverage.GeneratePaths(Arg.Any<FSM>()).Returns(threePaths);
        _reduction.Reduce(Arg.Any<TestSuite>(), Arg.Any<FSM>()).Returns(reducedSuite);

        var (suite, _, preSize, _) = new TestGenerator(_coverage, _reduction, _prioritization)
            .Generate(_fsm);

        Assert.That(preSize, Is.EqualTo(3));
        Assert.That(suite.Size, Is.EqualTo(1));
    }

    [Test]
    public void Generate_SuiteName_ContainsAllStrategyNames()
    {
        _coverage.GeneratePaths(Arg.Any<FSM>()).Returns(SinglePath(_fsm.Transitions[0]));

        var (suite, _, _, _) = new TestGenerator(_coverage, _reduction, _prioritization)
            .Generate(_fsm, "TestFSM");

        Assert.That(suite.Name, Does.Contain("MockCoverage"));
        Assert.That(suite.Name, Does.Contain("MockReduction"));
        Assert.That(suite.Name, Does.Contain("MockPrioritization"));
    }
}
