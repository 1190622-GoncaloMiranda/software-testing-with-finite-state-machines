using FinalArtefact.Core;

namespace FinalArtefact.TestModel;

public class TestCase
{
    private static int _idCounter = 0;

    public string Id { get; }
    public List<TestStep> Steps { get; }

    public IEnumerable<Transition> CoveredTransitions =>
        Steps.Select(s => s.Transition);

    public IEnumerable<State> VisitedStates =>
        Steps.Select(s => s.Transition.From)
             .Append(Steps.LastOrDefault()?.Transition.To!)
             .Where(s => s != null)
             .Distinct();

    public int Length => Steps.Count;

    public TestCase(IEnumerable<TestStep> steps, string? id = null)
    {
        Steps = steps.ToList();
        Id = id ?? $"TC{Interlocked.Increment(ref _idCounter):D4}";
    }

    public TestCase Clone() => new TestCase(Steps, Id + "_clone");

    public override string ToString() =>
        $"TestCase {Id} ({Length} steps): " +
        string.Join(" -> ", Steps.Select(s => s.Transition.From.Name)) +
        (Steps.Any() ? $" -> {Steps.Last().Transition.To.Name}" : "");
}
