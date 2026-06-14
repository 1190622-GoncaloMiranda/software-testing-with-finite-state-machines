using FinalArtefact.Core;

namespace FinalArtefact.TestModel;

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
