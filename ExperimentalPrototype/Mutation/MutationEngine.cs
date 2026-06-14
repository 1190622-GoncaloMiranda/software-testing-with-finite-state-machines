using ExperimentalPrototype.Core;

namespace ExperimentalPrototype.Mutation;

/// <summary>
/// Generates the complete set of mutants for an FSM by applying every registered
/// mutation operator. Each operator seeds a distinct type of fault.
///
/// Operators applied (by default):
///   - TransitionTargetMutation  (wrong next-state)
///   - OutputMutation            (wrong output)
///   - TransitionRemoval         (missing transition)
///   - TransitionAddition        (spurious transition)
///   - InputMutation             (wrong trigger input)
///   - InitialStateMutation      (wrong start state)
/// </summary>
public class MutationEngine
{
    private readonly List<IMutationOperator> _operators;

    /// <summary>Initialises with the default set of six mutation operators.</summary>
    public MutationEngine()
    {
        _operators = new List<IMutationOperator>
        {
            new TransitionTargetMutation(),
            new OutputMutation(),
            new TransitionRemoval(),
            new TransitionAddition(),
            new InputMutation(),
            new InitialStateMutation()
        };
    }

    /// <summary>Initialises with a custom set of mutation operators.</summary>
    public MutationEngine(IEnumerable<IMutationOperator> operators)
    {
        _operators = operators.ToList();
    }

    /// <summary>
    /// Generates all mutants from the given FSM by applying every registered operator.
    /// Returns a flat list of all produced mutants, labelled with their operator name.
    /// </summary>
    public List<Mutant> GenerateMutants(FSM fsm)
    {
        var all = new List<Mutant>();

        foreach (var op in _operators)
        {
            try
            {
                var mutants = op.Apply(fsm);
                all.AddRange(mutants);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[MutationEngine] Operator '{op.Name}' failed: {ex.Message}");
            }
        }

        return all;
    }

    /// <summary>Returns the names of all registered operators.</summary>
    public IEnumerable<string> OperatorNames =>
        _operators.Select(o => o.Name);
}
