using FinalArtefact.Core;

namespace FinalArtefact.Mutation;

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

    public MutationEngine(IEnumerable<IMutationOperator> operators)
    {
        _operators = operators.ToList();
    }

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

    public IEnumerable<string> OperatorNames =>
        _operators.Select(o => o.Name);
}
