using FinalArtefact.Core;
using FinalArtefact.Engine;
using FinalArtefact.TestModel;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Engine;

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
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);

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
        var badCase = new TestCase(new[] { new TestStep(new Transition(sA, sB, "bogus", "x")) });

        var result = _executor.Execute(badCase, fsm);

        Assert.That(result.HasError, Is.True);
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
        var badCase = new TestCase(new[] { new TestStep(new Transition(sA, sA, "bogus", "x")) });

        var result = _executor.Execute(badCase, fsm);

        Assert.That(FSMExecutor.IsPass(result), Is.False);
    }
}
