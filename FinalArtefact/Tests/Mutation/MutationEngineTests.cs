using FinalArtefact.Mutation;
using FinalArtefact.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace FinalArtefact.Tests.Mutation;

[TestFixture]
public class MutationEngineTests
{
    // ── MutationEngine with substituted operators ─────────────────────────────

    [Test]
    public void GenerateMutants_AggregatesMutantsFromAllOperators()
    {
        var fsm = FsmFixtures.Toggle2();
        var m1 = new Mutant(fsm.Clone(), "Op1", "desc1");
        var m2 = new Mutant(fsm.Clone(), "Op1", "desc2");
        var m3 = new Mutant(fsm.Clone(), "Op2", "desc3");

        var op1 = Substitute.For<IMutationOperator>();
        op1.Name.Returns("Op1");
        op1.Apply(Arg.Any<FinalArtefact.Core.FSM>()).Returns(new List<Mutant> { m1, m2 });

        var op2 = Substitute.For<IMutationOperator>();
        op2.Name.Returns("Op2");
        op2.Apply(Arg.Any<FinalArtefact.Core.FSM>()).Returns(new List<Mutant> { m3 });

        var mutants = new MutationEngine(new[] { op1, op2 }).GenerateMutants(fsm);

        Assert.That(mutants, Has.Count.EqualTo(3));
    }

    [Test]
    public void GenerateMutants_CallsApplyOnEachOperatorExactlyOnce()
    {
        var fsm = FsmFixtures.Toggle2();

        var op1 = Substitute.For<IMutationOperator>();
        op1.Name.Returns("Op1");
        op1.Apply(Arg.Any<FinalArtefact.Core.FSM>()).Returns(new List<Mutant>());

        var op2 = Substitute.For<IMutationOperator>();
        op2.Name.Returns("Op2");
        op2.Apply(Arg.Any<FinalArtefact.Core.FSM>()).Returns(new List<Mutant>());

        new MutationEngine(new[] { op1, op2 }).GenerateMutants(fsm);

        op1.Received(1).Apply(Arg.Any<FinalArtefact.Core.FSM>());
        op2.Received(1).Apply(Arg.Any<FinalArtefact.Core.FSM>());
    }

    [Test]
    public void GenerateMutants_OperatorReturnsEmpty_ContributesNoMutants()
    {
        var fsm = FsmFixtures.Toggle2();

        var op = Substitute.For<IMutationOperator>();
        op.Name.Returns("EmptyOp");
        op.Apply(Arg.Any<FinalArtefact.Core.FSM>()).Returns(new List<Mutant>());

        var mutants = new MutationEngine(new[] { op }).GenerateMutants(fsm);

        Assert.That(mutants, Is.Empty);
    }

    // ── Real operator behaviour ───────────────────────────────────────────────

    [Test]
    public void TransitionRemoval_ProducesExactlyOnePerTransition()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutants = new TransitionRemoval().Apply(fsm);

        Assert.That(mutants, Has.Count.EqualTo(fsm.Transitions.Count));
    }

    [Test]
    public void TransitionRemoval_EachMutantHasOneLessTransition()
    {
        var fsm = FsmFixtures.Linear3();
        var mutants = new TransitionRemoval().Apply(fsm);

        Assert.That(mutants, Has.All.Matches<Mutant>(m =>
            m.MutatedFSM.Transitions.Count == fsm.Transitions.Count - 1));
    }

    [Test]
    public void InitialStateMutation_ProducesOnePerAlternativeState()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutants = new InitialStateMutation().Apply(fsm);

        Assert.That(mutants, Has.Count.EqualTo(fsm.States.Count - 1));
    }

    [Test]
    public void InitialStateMutation_EachMutantHasDifferentInitialState()
    {
        var fsm = FsmFixtures.Linear3();
        var mutants = new InitialStateMutation().Apply(fsm);

        Assert.That(mutants, Has.All.Matches<Mutant>(m =>
            m.MutatedFSM.InitialState.Name != fsm.InitialState.Name));
    }

    [Test]
    public void Mutant_IsKilledFlag_DefaultsFalse()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutants = new TransitionRemoval().Apply(fsm);

        Assert.That(mutants, Has.All.Matches<Mutant>(m => !m.IsKilled));
    }

    [Test]
    public void Mutant_IsKilledFlag_CanBeSetAndReset()
    {
        var fsm = FsmFixtures.Toggle2();
        var mutant = new TransitionRemoval().Apply(fsm).First();

        mutant.IsKilled = true;
        Assert.That(mutant.IsKilled, Is.True);

        mutant.IsKilled = false;
        Assert.That(mutant.IsKilled, Is.False);
    }
}
