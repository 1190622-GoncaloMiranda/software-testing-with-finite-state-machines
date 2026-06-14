using FinalArtefact.Core;
using FinalArtefact.TestModel;

namespace FinalArtefact.Strategies.Prioritization;

/// <summary>
/// Coverage-based prioritization: orders test cases so that each successive test
/// maximises the number of new transitions covered (greedy additional coverage).
/// This is the standard "additional greedy" algorithm used in TCP literature.
/// </summary>
public class CoverageBasedPrioritization : IPrioritizationStrategy
{
    public string Name => "CoverageBasedPrioritization";

    public TestSuite Prioritize(TestSuite suite, FSM fsm)
    {
        var remaining = new List<TestCase>(suite.TestCases);
        var ordered = new List<TestCase>();
        var covered = new HashSet<string>();

        while (remaining.Count > 0)
        {
            var best = remaining
                .Select(tc => (tc, score: tc.CoveredTransitions
                    .Count(t => !covered.Contains(t.Id))))
                .OrderByDescending(x => x.score)
                .First().tc;

            ordered.Add(best);
            remaining.Remove(best);
            foreach (var t in best.CoveredTransitions)
                covered.Add(t.Id);
        }

        return new TestSuite($"{suite.Name}_CoverageOrdered", ordered);
    }
}

/// <summary>
/// Weighted transition prioritization: greedily orders test cases so that each
/// successive test maximises the cumulative weight of NEW transitions covered.
/// Weight = 1/frequency (rarer transitions across the suite receive higher weight),
/// so diverse, rare-path coverage is surfaced early.
/// Inspired by PSMT (Rechtberger et al., 2022).
/// </summary>
public class WeightedTransitionPrioritization : IPrioritizationStrategy
{
    public string Name => "WeightedTransitionPrioritization";

    public TestSuite Prioritize(TestSuite suite, FSM fsm)
    {
        var frequency = new Dictionary<string, int>();
        foreach (var tc in suite.TestCases)
            foreach (var t in tc.CoveredTransitions)
                frequency[t.Id] = frequency.GetValueOrDefault(t.Id, 0) + 1;

        double Weight(Transition t) =>
            frequency.TryGetValue(t.Id, out int f) ? 1.0 / f : 1.0;

        var remaining = new List<TestCase>(suite.TestCases);
        var ordered = new List<TestCase>();
        var covered = new HashSet<string>();

        while (remaining.Count > 0)
        {
            var best = remaining
                .Select(tc => (tc, score: tc.CoveredTransitions
                    .Where(t => !covered.Contains(t.Id))
                    .Sum(t => Weight(t))))
                .OrderByDescending(x => x.score)
                .First().tc;

            ordered.Add(best);
            remaining.Remove(best);
            foreach (var t in best.CoveredTransitions)
                covered.Add(t.Id);
        }

        return new TestSuite($"{suite.Name}_WeightedOrdered", ordered);
    }
}
