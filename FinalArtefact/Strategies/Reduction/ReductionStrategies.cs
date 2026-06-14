using FinalArtefact.Core;
using FinalArtefact.TestModel;

namespace FinalArtefact.Strategies.Reduction;

/// <summary>
/// Removes exact duplicate test cases (same sequence of transition IDs).
/// Fastest and safest reduction; never impacts coverage.
/// </summary>
public class DuplicateRemovalStrategy : IReductionStrategy
{
    public string Name => "DuplicateRemoval";

    public TestSuite Reduce(TestSuite suite, FSM fsm)
    {
        var seen = new HashSet<string>();
        var reduced = new List<TestCase>();

        foreach (var tc in suite.TestCases)
        {
            var fingerprint = string.Join("|", tc.Steps.Select(s => s.Transition.Id));
            if (seen.Add(fingerprint))
                reduced.Add(tc);
        }

        return new TestSuite($"{suite.Name}_NoDuplicates", reduced);
    }
}

/// <summary>
/// Greedy coverage-preserving reduction.
/// Iteratively removes test cases that do not contribute any unique transition coverage.
/// Guarantees that the reduced suite retains 100% of the transition coverage
/// achievable by the original suite.
/// </summary>
public class CoveragePreservingReductionStrategy : IReductionStrategy
{
    public string Name => "CoveragePreservingReduction";

    public TestSuite Reduce(TestSuite suite, FSM fsm)
    {
        var globalCoverage = suite.GetCoveredTransitionIds();
        var remaining = new List<TestCase>(suite.TestCases);
        var selected = new List<TestCase>();
        var coveredSoFar = new HashSet<string>();

        while (coveredSoFar.Count < globalCoverage.Count && remaining.Count > 0)
        {
            var best = remaining
                .Select(tc => (tc, newCoverage: tc.CoveredTransitions
                    .Select(t => t.Id)
                    .Count(id => !coveredSoFar.Contains(id))))
                .OrderByDescending(x => x.newCoverage)
                .First();

            if (best.newCoverage == 0)
                break;

            selected.Add(best.tc);
            foreach (var id in best.tc.CoveredTransitions.Select(t => t.Id))
                coveredSoFar.Add(id);
            remaining.Remove(best.tc);
        }

        return new TestSuite($"{suite.Name}_CoveragePreserved", selected);
    }
}

/// <summary>
/// Coverage-preserving reduction followed by BMI-based similarity filtering.
///
/// Pipeline:
///   1. Apply greedy coverage-preserving reduction.
///   2. On that minimal suite, iterate through the test cases and only keep
///      a candidate if no already-selected case has a normalised BMI similarity
///      above the threshold.
///
/// Similarity measure — Biased Mutual Information (Ibias et al.):
///   bmi(t1; t2) = Σ_{x ∈ t2} n_x · log₂(m_x + 1) / m_x
///
/// The normalised form bmi(t1,t2) / bmi(t2,t2) ∈ [0,1] by construction.
/// </summary>
public class SimilarityReductionStrategy : IReductionStrategy
{
    public string Name => "SimilarityReduction";
    private readonly double _similarityThreshold;
    private readonly CoveragePreservingReductionStrategy _coveragePreserving = new();

    public SimilarityReductionStrategy(double similarityThreshold = 0.8)
    {
        _similarityThreshold = similarityThreshold;
    }

    public TestSuite Reduce(TestSuite suite, FSM fsm)
    {
        var minimal = _coveragePreserving.Reduce(suite, fsm);
        var labelCounts = BuildLabelCounts(fsm);

        var selected = new List<TestCase>();
        var selectedPairs = new List<List<(string Input, string Output)>>();

        foreach (var candidate in minimal.TestCases)
        {
            var candidatePairs = GetIoPairs(candidate);

            bool tooSimilar = selectedPairs.Any(sp =>
                NormalizedBmi(sp, candidatePairs, labelCounts) >= _similarityThreshold);

            if (!tooSimilar)
            {
                selected.Add(candidate);
                selectedPairs.Add(candidatePairs);
            }
        }

        return new TestSuite($"{suite.Name}_SimilarityReduced", selected);
    }

    private static List<(string Input, string Output)> GetIoPairs(TestCase tc) =>
        [.. tc.Steps.Select(s => (s.Transition.Input, s.Transition.Output))];

    private static Dictionary<(string Input, string Output), int> BuildLabelCounts(FSM fsm)
    {
        var counts = new Dictionary<(string Input, string Output), int>();
        foreach (var t in fsm.Transitions)
        {
            var key = (t.Input, t.Output);
            counts.TryGetValue(key, out int c);
            counts[key] = c + 1;
        }
        return counts;
    }

    private static double NormalizedBmi(
        List<(string Input, string Output)> t1Pairs,
        List<(string Input, string Output)> t2Pairs,
        Dictionary<(string Input, string Output), int> labelCounts)
    {
        if (t2Pairs.Count == 0) return 1.0;

        var t1Counts = t1Pairs.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

        double numerator   = 0.0;
        double denominator = 0.0;

        foreach (var group in t2Pairs.GroupBy(x => x))
        {
            var x     = group.Key;
            int nxT2  = group.Count();
            t1Counts.TryGetValue(x, out int nxT1);

            labelCounts.TryGetValue(x, out int mx);
            if (mx == 0) mx = 1;

            double weight  = Math.Log2(mx + 1.0) / mx;
            denominator   += nxT2 * weight;
            numerator     += Math.Min(nxT1, nxT2) * weight;
        }

        return denominator == 0.0 ? 1.0 : numerator / denominator;
    }
}
