using FinalArtefact.Core;

namespace FinalArtefact.Mutation;

public interface IMutationOperator
{
    string Name { get; }
    List<Mutant> Apply(FSM fsm);
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mutation operator: changes the target (To) state of one transition to a
/// different state. Simulates incorrect next-state in the specification.
/// </summary>
public class TransitionTargetMutation : IMutationOperator
{
    public string Name => "TransitionTargetMutation";

    public List<Mutant> Apply(FSM fsm)
    {
        var mutants = new List<Mutant>();

        foreach (var transition in fsm.Transitions)
        {
            foreach (var altState in fsm.States.Where(s => !s.Equals(transition.To)))
            {
                var newTransitions = fsm.Transitions.Select(t =>
                    t.Id == transition.Id
                        ? new Transition(t.From, altState, t.Input, t.Output)
                        : t);

                var mutant = fsm.WithTransitions(newTransitions);
                mutants.Add(new Mutant(mutant, Name,
                    $"{transition} -> target changed to {altState}"));
            }
        }

        return mutants;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mutation operator: changes the output of one transition to a different value.
/// Simulates incorrect output production (output fault).
///
/// If the FSM's output alphabet has fewer than two symbols no mutants are produced.
/// </summary>
public class OutputMutation : IMutationOperator
{
    public string Name => "OutputMutation";

    public List<Mutant> Apply(FSM fsm)
    {
        var mutants = new List<Mutant>();
        var allOutputs = fsm.Transitions.Select(t => t.Output).Distinct().ToList();
        if (allOutputs.Count < 2) return mutants;

        foreach (var transition in fsm.Transitions)
        {
            foreach (var altOutput in allOutputs.Where(o => o != transition.Output))
            {
                var newTransitions = fsm.Transitions.Select(t =>
                    t.Id == transition.Id
                        ? new Transition(t.From, t.To, t.Input, altOutput)
                        : t);

                var mutant = fsm.WithTransitions(newTransitions);
                mutants.Add(new Mutant(mutant, Name,
                    $"{transition} -> output changed to '{altOutput}'"));
            }
        }

        return mutants;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mutation operator: removes one transition from the FSM.
/// Simulates a missing transition (omission fault).
/// </summary>
public class TransitionRemoval : IMutationOperator
{
    public string Name => "TransitionRemoval";

    public List<Mutant> Apply(FSM fsm)
    {
        var mutants = new List<Mutant>();

        foreach (var transition in fsm.Transitions)
        {
            var newTransitions = fsm.Transitions.Where(t => t.Id != transition.Id);
            var mutant = fsm.WithTransitions(newTransitions);
            mutants.Add(new Mutant(mutant, Name,
                $"removed transition {transition}"));
        }

        return mutants;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mutation operator: adds a spurious transition between two existing states.
/// Simulates an extra, unintended transition (addition fault).
/// Only adds transitions on (from, input) pairs that are currently free — otherwise
/// the added transition would be shadowed by an existing one and never fire.
///
/// TransitionAddition mutants are classified as strategy-unreachable by the
/// FinalArtefact pipeline because test sequences derived from the original FSM
/// never send inputs that are undefined at the current state in the original FSM,
/// making it impossible to trigger the added transition.
/// </summary>
public class TransitionAddition : IMutationOperator
{
    public string Name => "TransitionAddition";

    public List<Mutant> Apply(FSM fsm)
    {
        var mutants = new List<Mutant>();
        var allInputs = fsm.Transitions.Select(t => t.Input).Distinct().ToList();

        foreach (var from in fsm.States)
        {
            var definedInputs = fsm.GetOutgoingTransitions(from)
                .Select(t => t.Input).ToHashSet();
            var freeInputs = allInputs.Where(i => !definedInputs.Contains(i)).ToList();
            if (freeInputs.Count == 0) continue;

            foreach (var to in fsm.States.Where(s => !s.Equals(from)))
            {
                var input = freeInputs[0];
                var newTransitions = fsm.Transitions.Append(
                    new Transition(from, to, input, "extra_output"));

                var mutant = fsm.WithTransitions(newTransitions);
                mutants.Add(new Mutant(mutant, Name,
                    $"added transition {from}-[{input}]->{to}"));
            }
        }

        return mutants;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mutation operator: changes the input label of one transition to a different label.
/// Simulates a wrong trigger condition (input fault).
///
/// If the FSM's input alphabet has fewer than two symbols no mutants are produced.
/// </summary>
public class InputMutation : IMutationOperator
{
    public string Name => "InputMutation";

    public List<Mutant> Apply(FSM fsm)
    {
        var mutants = new List<Mutant>();
        var allInputs = fsm.Transitions.Select(t => t.Input).Distinct().ToList();
        if (allInputs.Count < 2) return mutants;

        foreach (var transition in fsm.Transitions)
        {
            foreach (var altInput in allInputs.Where(i => i != transition.Input))
            {
                var newTransitions = fsm.Transitions.Select(t =>
                    t.Id == transition.Id
                        ? new Transition(t.From, t.To, altInput, t.Output)
                        : t);

                var mutant = fsm.WithTransitions(newTransitions);
                mutants.Add(new Mutant(mutant, Name,
                    $"{transition} -> input changed to '{altInput}'"));
            }
        }

        return mutants;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mutation operator: changes the initial state of the FSM to a different state.
/// Simulates an incorrect starting condition.
/// </summary>
public class InitialStateMutation : IMutationOperator
{
    public string Name => "InitialStateMutation";

    public List<Mutant> Apply(FSM fsm)
    {
        var mutants = new List<Mutant>();

        foreach (var altState in fsm.States.Where(s => !s.Equals(fsm.InitialState)))
        {
            var stateMap = fsm.States.ToDictionary(s => s.Name, s => new State(s.Name));
            var newInitial = stateMap[altState.Name];
            var newTransitions = fsm.Transitions.Select(t =>
                new Transition(stateMap[t.From.Name], stateMap[t.To.Name], t.Input, t.Output));

            var mutant = new FSM(newInitial, stateMap.Values, newTransitions)
            {
                Name = fsm.Name
            };

            mutants.Add(new Mutant(mutant, Name,
                $"initial state changed from {fsm.InitialState} to {altState}"));
        }

        return mutants;
    }
}
