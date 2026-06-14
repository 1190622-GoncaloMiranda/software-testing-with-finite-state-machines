using ExperimentalPrototype.Core;

namespace ExperimentalPrototype.TestModel;

/// <summary>
/// Represents a single step in a test case: the transition to be fired.
/// In a classical FSM the transition's input symbol is the input; there is
/// no separate concrete data payload.
/// </summary>
public class TestStep
{
    public Transition Transition { get; }

    public TestStep(Transition transition)
    {
        Transition = transition ?? throw new ArgumentNullException(nameof(transition));
    }

    public override string ToString() =>
        $"[{Transition.From} --({Transition.Input})--> {Transition.To}, expected output: {Transition.Output}]";
}
