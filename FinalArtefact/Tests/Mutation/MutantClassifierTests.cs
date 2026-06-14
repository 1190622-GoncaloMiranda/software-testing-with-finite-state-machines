using FinalArtefact.Mutation;
using FinalArtefact.Tests.Helpers;
using NUnit.Framework;

namespace FinalArtefact.Tests.Mutation;

[TestFixture]
public class MutantClassifierTests
{
    private static Mutant Make(string operatorName, bool isKilled)
    {
        var fsm = FsmFixtures.Toggle2();
        return new Mutant(fsm.Clone(), operatorName, "test") { IsKilled = isKilled };
    }

    // ── Category assignment ───────────────────────────────────────────────────

    [Test]
    public void Classify_KilledMutant_PlacedInKilledList()
    {
        var mutants = new[] { Make("OutputMutation", isKilled: true) };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.Killed, Has.Count.EqualTo(1));
        Assert.That(result.StrategyUnreachable, Is.Empty);
        Assert.That(result.Surviving, Is.Empty);
    }

    [Test]
    public void Classify_TransitionAdditionNotKilled_IsStrategyUnreachable()
    {
        var mutants = new[] { Make("TransitionAddition", isKilled: false) };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.StrategyUnreachable, Has.Count.EqualTo(1));
        Assert.That(result.Killed, Is.Empty);
        Assert.That(result.Surviving, Is.Empty);
    }

    [Test]
    public void Classify_OtherOperatorNotKilled_IsSurviving()
    {
        var mutants = new[] { Make("OutputMutation", isKilled: false) };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.Surviving, Has.Count.EqualTo(1));
        Assert.That(result.Killed, Is.Empty);
        Assert.That(result.StrategyUnreachable, Is.Empty);
    }

    [Test]
    public void Classify_EmptyMutantList_ReturnsEmptyCategories()
    {
        var result = MutantClassifier.Classify(Array.Empty<Mutant>());

        Assert.That(result.Killed, Is.Empty);
        Assert.That(result.StrategyUnreachable, Is.Empty);
        Assert.That(result.Surviving, Is.Empty);
    }

    [Test]
    public void Classify_MixedMutants_CorrectlyPartitions()
    {
        var mutants = new[]
        {
            Make("OutputMutation",      isKilled: true),
            Make("TransitionRemoval",   isKilled: true),
            Make("TransitionAddition",  isKilled: false),
            Make("InputMutation",       isKilled: false),
        };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.Killed,              Has.Count.EqualTo(2));
        Assert.That(result.StrategyUnreachable, Has.Count.EqualTo(1));
        Assert.That(result.Surviving,           Has.Count.EqualTo(1));
    }

    // ── Adjusted mutation score ───────────────────────────────────────────────

    [Test]
    public void AdjustedMutationScore_ExcludesUnreachableFromDenominator()
    {
        // 2 killed, 1 unreachable, 0 surviving → adjusted score = 2/2 = 1.0
        var mutants = new[]
        {
            Make("OutputMutation",     isKilled: true),
            Make("TransitionRemoval",  isKilled: true),
            Make("TransitionAddition", isKilled: false),
        };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.AdjustedMutationScore, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void AdjustedMutationScore_SomeSurviving_IsLessThanOne()
    {
        // 1 killed, 1 surviving, 1 unreachable → adjusted = 1/2 = 0.5
        var mutants = new[]
        {
            Make("OutputMutation",     isKilled: true),
            Make("InputMutation",      isKilled: false),
            Make("TransitionAddition", isKilled: false),
        };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.AdjustedMutationScore, Is.EqualTo(0.5).Within(1e-10));
    }

    [Test]
    public void AdjustedMutationScore_AllStrategyUnreachable_ReturnsOne()
    {
        var mutants = new[]
        {
            Make("TransitionAddition", isKilled: false),
            Make("TransitionAddition", isKilled: false),
        };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.AdjustedMutationScore, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void TotalClassifiable_IsKilledPlusSurviving()
    {
        var mutants = new[]
        {
            Make("OutputMutation",     isKilled: true),
            Make("InputMutation",      isKilled: false),
            Make("TransitionAddition", isKilled: false),
        };

        var result = MutantClassifier.Classify(mutants);

        Assert.That(result.TotalClassifiable, Is.EqualTo(result.Killed.Count + result.Surviving.Count));
    }
}
