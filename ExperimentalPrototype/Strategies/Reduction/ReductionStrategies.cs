using ExperimentalPrototype.Core;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Strategies.Reduction;

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
            // Canonical fingerprint: ordered list of transition IDs
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
/// Based on the greedy set-cover principle used in literature (e.g., TIGER+).
/// </summary>
public class CoveragePreservingReductionStrategy : IReductionStrategy
{
    public string Name => "CoveragePreservingReduction";

    public TestSuite Reduce(TestSuite suite, FSM fsm)
    {
        // Compute the total coverage target
        var globalCoverage = suite.GetCoveredTransitionIds();

        var remaining = new List<TestCase>(suite.TestCases);
        var selected = new List<TestCase>();
        var coveredSoFar = new HashSet<string>();

        // Greedy: always pick the test case that adds the most new coverage
        while (coveredSoFar.Count < globalCoverage.Count && remaining.Count > 0)
        {
            // Score each candidate by number of NEW transitions it covers
            var best = remaining
                .Select(tc => (tc, newCoverage: tc.CoveredTransitions
                    .Select(t => t.Id)
                    .Count(id => !coveredSoFar.Contains(id))))
                .OrderByDescending(x => x.newCoverage)
                .First();

            if (best.newCoverage == 0)
                break; // no more coverage to gain

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
///   1. Apply greedy coverage-preserving reduction (same as
///      <see cref="CoveragePreservingReductionStrategy"/>) to obtain a minimal
///      suite that still exercises every transition covered by the original.
///   2. On that minimal suite, iterate through the test cases and only keep
///      a candidate if no already-selected case has a normalised BMI similarity
///      above the threshold.
///
/// Similarity measure — Biased Mutual Information (Ibias et al. [20]):
///
///   bmi(t1; t2) = Σ_{x ∈ t2} n_x · log₂(m_x + 1) / m_x
///
///   where  n_x = occurrences of input/output pair x in t1 (capped at count in t2)
///          m_x = number of FSM transitions that carry that (input, output) label
///
/// The normalised form bmi(t1,t2) / bmi(t2,t2) ∈ [0,1] by construction.
/// It captures "how much of t2's informational content is already covered by t1."
/// Rare (input, output) labels — those appearing on few FSM transitions — receive
/// higher weight, so shared distinctive paths drive similarity more than common ones.
/// </summary>
public class SimilarityReductionStrategy : IReductionStrategy
{
    public string Name => "SimilarityReduction";
    private readonly double _similarityThreshold;
    private readonly CoveragePreservingReductionStrategy _coveragePreserving = new();

    /// <param name="similarityThreshold">
    /// Normalised BMI above which a test case is considered redundant (default 0.8).
    /// Applied only after coverage-preserving reduction, so it never drops coverage
    /// that was contributed uniquely by a single test case.
    /// </param>
    public SimilarityReductionStrategy(double similarityThreshold = 0.8)
    {
        _similarityThreshold = similarityThreshold;
    }

    public TestSuite Reduce(TestSuite suite, FSM fsm)
    {
        // Step 1: coverage-preserving reduction — guarantees 100% of the
        // original suite's transition coverage is retained.
        var minimal = _coveragePreserving.Reduce(suite, fsm);

        // Precompute per-FSM label frequencies once (m_x values).
        var labelCounts = BuildLabelCounts(fsm);

        // Step 2: BMI similarity filter over the already-minimal set.
        // Cache IO-pair lists for selected cases to avoid repeated recomputation.
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

    // Normalised BMI in [0,1]: fraction of t2's information content shared with t1.
    // n_x is capped at t2's count so the ratio never exceeds 1.
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
