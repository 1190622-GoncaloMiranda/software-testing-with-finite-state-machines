using FinalArtefact.Tests.Helpers;
using FinalArtefact.Validation;
using FinalArtefact.Core;
using NUnit.Framework;

namespace FinalArtefact.Tests.Validation;

[TestFixture]
public class FsmValidatorTests
{
    private FsmValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new FsmValidator();

    // ── Valid FSM ─────────────────────────────────────────────────────────────

    [Test]
    public void Validate_WellFormedDeterministicFsm_IsValidWithNoWarnings()
    {
        var result = _validator.Validate(FsmFixtures.Linear3());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Is.Empty);
    }

    // ── Error conditions ──────────────────────────────────────────────────────

    [Test]
    public void Validate_InitialStateNotInStatesList_ReturnsError()
    {
        var sA = new State("A");
        var sB = new State("B");
        var sGhost = new State("Ghost");
        // sGhost is listed as initial but not in states
        var fsm = new FSM(sGhost, new[] { sA, sB }, new[]
        {
            new Transition(sA, sB, "go", "ok")
        });

        var result = _validator.Validate(fsm);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.GreaterThan(0));
    }

    [Test]
    public void Validate_NonDeterministicFsm_ReturnsError()
    {
        var sA = new State("A");
        var sB = new State("B");
        var sC = new State("C");
        // Two outgoing transitions from sA with the same input "go"
        var fsm = new FSM(sA, new[] { sA, sB, sC }, new[]
        {
            new Transition(sA, sB, "go", "ok"),
            new Transition(sA, sC, "go", "err")
        });

        var result = _validator.Validate(fsm);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("Non-determinism") || e.Contains("non-determinism")), Is.True);
    }

    // ── Warning conditions ────────────────────────────────────────────────────

    [Test]
    public void Validate_UnreachableState_AddsWarning()
    {
        var sA = new State("A");
        var sB = new State("B");
        var sOrphan = new State("Orphan");
        var fsm = new FSM(sA, new[] { sA, sB, sOrphan }, new[]
        {
            new Transition(sA, sB, "go", "ok")
        });

        var result = _validator.Validate(fsm);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Warnings.Any(w => w.Contains("Orphan")), Is.True);
    }

    [Test]
    public void Validate_MutationScorePreserving_ExceedsThreshold_AddsWarning()
    {
        // Build an FSM with transitions > threshold
        var states = Enumerable.Range(0, 10).Select(i => new State($"S{i}")).ToArray();
        var transitions = states
            .SelectMany((s, i) => new[]
            {
                new Transition(s, states[(i + 1) % states.Length], $"a{i}", $"out{i}"),
                new Transition(s, states[(i + 2) % states.Length], $"b{i}", $"out{i}b"),
                new Transition(s, states[(i + 3) % states.Length], $"c{i}", $"out{i}c"),
            })
            .ToArray(); // 30 transitions

        var fsm = new FSM(states[0], states, transitions);

        var result = _validator.Validate(fsm, reductionStrategyType: "MutationScorePreserving", fsmSizeThreshold: 20);

        Assert.That(result.Warnings.Any(w => w.Contains("MutationScorePreserving") || w.Contains("threshold")), Is.True);
    }

    [Test]
    public void Validate_MutationScorePreserving_WithinThreshold_NoWarning()
    {
        var result = _validator.Validate(
            FsmFixtures.Toggle2(),
            reductionStrategyType: "MutationScorePreserving",
            fsmSizeThreshold: 50);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void Validate_DifferentReductionStrategy_DoesNotWarnAboutSize()
    {
        var states = Enumerable.Range(0, 5).Select(i => new State($"S{i}")).ToArray();
        var transitions = states
            .Select((s, i) => new Transition(s, states[(i + 1) % states.Length], $"a{i}", $"o{i}"))
            .ToArray();
        var fsm = new FSM(states[0], states, transitions);

        // CoveragePreserving reduction — size threshold warning should NOT fire
        var result = _validator.Validate(fsm, reductionStrategyType: "CoveragePreserving", fsmSizeThreshold: 1);

        Assert.That(result.Warnings.Any(w => w.Contains("MutationScorePreserving")), Is.False);
    }
}
