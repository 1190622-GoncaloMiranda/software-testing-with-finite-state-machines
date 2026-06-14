using ExperimentalPrototype.Core;
using ExperimentalPrototype.Engine;
using ExperimentalPrototype.TestModel;
using ExperimentalPrototype.Tests.Helpers;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.Engine;

[TestFixture]
public class FsmExecutorTests
{
    private FSMExecutor _executor = null!;

    [SetUp]
    public void SetUp() => _executor = new FSMExecutor();

    [Test]
    public void Execute_ValidSequence_ProducesExpectedOutputsInOrder()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions); // connect → send → reset

        var result = _executor.Execute(tc, fsm);

        Assert.That(result.HasError, Is.False);
        Assert.That(result.Outputs, Is.EqualTo(new[] { "ack", "ok", "done" }));
    }

    [Test]
    public void Execute_UnknownInput_SetsHasErrorAndStops()
    {
        var fsm = FsmFixtures.Linear3();
        var sA = fsm.InitialState;
        var sB = new State("B");
        var badStep = new TestCase(new[] { new TestStep(new Transition(sA, sB, "bogus", "x")) });

        var result = _executor.Execute(badStep, fsm);

        Assert.That(result.HasError, Is.True);
        Assert.That(result.Outputs, Has.Count.EqualTo(1));
    }

    [Test]
    public void Execute_EmptyTestCase_ReturnsNoOutputsAndNoError()
    {
        var fsm = FsmFixtures.Linear3();
        var empty = new TestCase(Enumerable.Empty<TestStep>());

        var result = _executor.Execute(empty, fsm);

        Assert.That(result.HasError, Is.False);
        Assert.That(result.Outputs, Is.Empty);
    }

    [Test]
    public void Execute_FinalState_MatchesLastTransitionTarget()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions.Take(2)); // A→B→C

        var result = _executor.Execute(tc, fsm);

        Assert.That(result.FinalState.Name, Is.EqualTo("C"));
    }

    [Test]
    public void IsPass_OutputsMatchExpected_ReturnsTrue()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);

        var result = _executor.Execute(tc, fsm);

        Assert.That(FSMExecutor.IsPass(result), Is.True);
    }

    [Test]
    public void IsPass_ErrorResult_ReturnsFalse()
    {
        var fsm = FsmFixtures.Linear3();
        var sA = fsm.InitialState;
        var badStep = new TestCase(new[] { new TestStep(new Transition(sA, sA, "bogus", "x")) });

        var result = _executor.Execute(badStep, fsm);

        Assert.That(FSMExecutor.IsPass(result), Is.False);
    }
}
