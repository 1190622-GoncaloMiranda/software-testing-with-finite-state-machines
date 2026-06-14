using FinalArtefact.Mutation;

namespace FinalArtefact.Mutation;

public class MutantClassificationResult
{
    public IReadOnlyList<Mutant> Killed { get; }
    public IReadOnlyList<Mutant> StrategyUnreachable { get; }
    public IReadOnlyList<Mutant> Surviving { get; }

    /// <summary>
    /// Mutants that are classifiable (killed or surviving — strategy-unreachable excluded).
    /// Used as the denominator for the adjusted mutation score.
    /// </summary>
    public int TotalClassifiable => Killed.Count + Surviving.Count;

    /// <summary>
    /// Mutation score with strategy-unreachable mutants excluded from the denominator.
    /// This gives a more accurate picture of fault-detection capability under the
    /// chosen coverage criterion.
    /// </summary>
    public double AdjustedMutationScore =>
        TotalClassifiable == 0 ? 1.0 : (double)Killed.Count / TotalClassifiable;

    public MutantClassificationResult(
        IReadOnlyList<Mutant> killed,
        IReadOnlyList<Mutant> strategyUnreachable,
        IReadOnlyList<Mutant> surviving)
    {
        Killed = killed;
        StrategyUnreachable = strategyUnreachable;
        Surviving = surviving;
    }
}

/// <summary>
/// Classifies mutants into three categories after mutation testing:
///
///   Killed             — detected by at least one test case in the final suite.
///   Strategy-unreachable — TransitionAddition mutants that cannot be killed by
///                          any test generated from the original FSM. These mutants
///                          add a transition on an input that is not defined at the
///                          source state in the original FSM; since test sequences
///                          only fire inputs defined in the original model, the
///                          added transition is never triggered.
///   Surviving          — not killed and not strategy-unreachable (genuine gaps in
///                        fault detection — candidates for manual review).
/// </summary>
public static class MutantClassifier
{
    private const string TransitionAdditionOperator = "TransitionAddition";

    public static MutantClassificationResult Classify(IReadOnlyList<Mutant> mutants)
    {
        var killed             = new List<Mutant>();
        var strategyUnreachable = new List<Mutant>();
        var surviving          = new List<Mutant>();

        foreach (var m in mutants)
        {
            if (m.IsKilled)
                killed.Add(m);
            else if (m.OperatorName == TransitionAdditionOperator)
                strategyUnreachable.Add(m);
            else
                surviving.Add(m);
        }

        return new MutantClassificationResult(killed, strategyUnreachable, surviving);
    }
}
