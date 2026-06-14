namespace ExperimentalPrototype.Core;

/// <summary>
/// Represents a transition between two states in a Finite State Machine.
/// Each transition is triggered by an input symbol and produces an output symbol.
///
/// Transitions follow the classical Mealy model: inputs and outputs are opaque
/// symbolic labels with no associated variables, guards, or data values.
/// Data-dependent or symbolic transitions (EFSM / SFSM variants) are out of scope.
/// </summary>
public class Transition
{
    public State From { get; }
    public State To { get; }
    public string Input { get; }
    public string Output { get; }

    // Unique identifier for referencing this transition in metrics and mutation
    public string Id => $"{From.Name}--{Input}-->{To.Name}";

    public Transition(State from, State to, string input, string output)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public override string ToString() => $"({From} --[{Input}/{Output}]--> {To})";

    public override bool Equals(object? obj) =>
        obj is Transition other &&
        From.Equals(other.From) &&
        To.Equals(other.To) &&
        Input == other.Input &&
        Output == other.Output;

    public override int GetHashCode() =>
        HashCode.Combine(From, To, Input, Output);
}
