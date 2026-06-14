using FinalArtefact.Core;

namespace FinalArtefact.Validation;

public class ValidationResult
{
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool IsValid => Errors.Count == 0;

    public ValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings)
    {
        Errors = errors.ToList();
        Warnings = warnings.ToList();
    }
}

/// <summary>
/// Validates an FSM model before processing.
///
/// Checks performed:
///   - At least one state exists
///   - Initial state is present in the states list
///   - FSM is deterministic (no duplicate (state, input) pairs)
///   - All states are reachable from the initial state (warning only)
///   - FSM size vs. MutationScorePreserving threshold (warning only)
/// </summary>
public class FsmValidator
{
    public ValidationResult Validate(
        FSM fsm,
        string? reductionStrategyType = null,
        int fsmSizeThreshold = 50)
    {
        var errors   = new List<string>();
        var warnings = new List<string>();

        if (!fsm.States.Any())
        {
            errors.Add("FSM has no states.");
            return new ValidationResult(errors, warnings);
        }

        if (!fsm.States.Any(s => s.Name == fsm.InitialState.Name))
            errors.Add($"Initial state '{fsm.InitialState.Name}' is not in the states list.");

        // Determinism check: at most one outgoing transition per (state, input)
        foreach (var state in fsm.States)
        {
            var inputs = fsm.GetOutgoingTransitions(state).Select(t => t.Input).ToList();
            var duplicates = inputs
                .GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
                errors.Add(
                    $"Non-determinism at state '{state.Name}': " +
                    $"input(s) [{string.Join(", ", duplicates)}] have multiple outgoing transitions.");
        }

        // Reachability check (warning only — partial FSMs are acceptable)
        var reachable = fsm.GetReachableStates().Select(s => s.Name).ToHashSet();
        var unreachable = fsm.States
            .Where(s => !reachable.Contains(s.Name))
            .Select(s => s.Name)
            .ToList();

        if (unreachable.Any())
            warnings.Add(
                $"Unreachable states detected (will not appear in generated tests): " +
                string.Join(", ", unreachable));

        // Size warning for MutationScorePreserving reduction
        if (reductionStrategyType == "MutationScorePreserving" &&
            fsm.Transitions.Count > fsmSizeThreshold)
        {
            warnings.Add(
                $"FSM has {fsm.Transitions.Count} transitions, exceeding the " +
                $"MutationScorePreserving reduction threshold of {fsmSizeThreshold}. " +
                "Generation may be slow. Consider switching to CoveragePreserving or SimilarityReduction.");
        }

        return new ValidationResult(errors, warnings);
    }
}
