using FinalArtefact.Core;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Core;

[TestFixture]
public class FsmTests
{
    // ── State ────────────────────────────────────────────────────────────────

    [Test]
    public void State_SameName_AreEqual()
    {
        Assert.That(new State("A"), Is.EqualTo(new State("A")));
    }

    [Test]
    public void State_DifferentName_AreNotEqual()
    {
        Assert.That(new State("A"), Is.Not.EqualTo(new State("B")));
    }

    [Test]
    public void State_ToString_ContainsName()
    {
        Assert.That(new State("MyState").ToString(), Does.Contain("MyState"));
    }

    // ── Transition ───────────────────────────────────────────────────────────

    [Test]
    public void Transition_Id_IsNotEmpty()
    {
        var sA = new State("A");
        var sB = new State("B");
        Assert.That(new Transition(sA, sB, "in", "out").Id, Is.Not.Empty);
    }

    // ── FSM ──────────────────────────────────────────────────────────────────

    [Test]
    public void FSM_GetOutgoingTransitions_ReturnsOnlyTransitionsFromThatState()
    {
        var fsm = FsmFixtures.Linear3();
        var fromA = fsm.GetOutgoingTransitions(fsm.InitialState);

        Assert.That(fromA, Has.Count.EqualTo(1));
        Assert.That(fromA[0].Input, Is.EqualTo("connect"));
    }

    [Test]
    public void FSM_GetTransition_FindsByStateAndInput()
    {
        var fsm = FsmFixtures.Linear3();
        var t = fsm.GetTransition(fsm.InitialState, "connect");

        Assert.That(t, Is.Not.Null);
        Assert.That(t!.Output, Is.EqualTo("ack"));
    }

    [Test]
    public void FSM_GetTransition_ReturnsNullForUnknownInput()
    {
        var fsm = FsmFixtures.Linear3();
        Assert.That(fsm.GetTransition(fsm.InitialState, "unknown"), Is.Null);
    }

    [Test]
    public void FSM_GetReachableStates_ContainsAllReachableStates()
    {
        var fsm = FsmFixtures.Linear3();
        var names = fsm.GetReachableStates().Select(s => s.Name).ToHashSet();

        Assert.That(names, Does.Contain("A"));
        Assert.That(names, Does.Contain("B"));
        Assert.That(names, Does.Contain("C"));
    }

    [Test]
    public void FSM_GetReachableStates_ExcludesUnreachableState()
    {
        var sA = new State("A");
        var sB = new State("B");
        var sOrphan = new State("Orphan");
        var fsm = new FSM(sA, new[] { sA, sB, sOrphan }, new[]
        {
            new Transition(sA, sB, "go", "gone")
        });

        var names = fsm.GetReachableStates().Select(s => s.Name);
        Assert.That(names, Does.Not.Contain("Orphan"));
    }

    [Test]
    public void FSM_Clone_ProducesIndependentCopy()
    {
        var fsm = FsmFixtures.Linear3();
        var clone = fsm.Clone();

        Assert.That(clone, Is.Not.SameAs(fsm));
        Assert.That(clone.States.Count, Is.EqualTo(fsm.States.Count));
        Assert.That(clone.Transitions.Count, Is.EqualTo(fsm.Transitions.Count));
    }

    [Test]
    public void FSM_WithTransitions_ReplacesTransitionList()
    {
        var fsm = FsmFixtures.Linear3();
        var mutant = fsm.WithTransitions(fsm.Transitions.Take(1));

        Assert.That(mutant.Transitions, Has.Count.EqualTo(1));
        Assert.That(mutant.States.Count, Is.EqualTo(fsm.States.Count));
    }
}
