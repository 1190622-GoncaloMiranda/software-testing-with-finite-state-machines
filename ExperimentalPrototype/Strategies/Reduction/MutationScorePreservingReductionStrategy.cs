using ExperimentalPrototype.Core;
using ExperimentalPrototype.Engine;
using ExperimentalPrototype.Mutation;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Strategies.Reduction;

/// <summary>
/// Mutation-score-preserving reduction inspired by the NDOE (Non-Deterministic Output-Error)
/// encoding approach described by Ramada et al.
///
/// Unlike CoveragePreservingReduction, which greedily maximises transition coverage,
/// this strategy optimises directly for fault-detection capability:
///
///   1. Generates the full mutant set for the FSM (all six operators).
///   2. Builds a kill map: for each test case, the set of mutant indices it kills.
///   3. Finds all killable mutants (killed by at least one test case in the suite).
///   4. Greedily selects test cases scored by:
///
///        Score = α · (uniqueKills / totalMutants)
///               - β · (length / maxLength)
///               - γ · resetsAdded
///
///      where:
///        uniqueKills  = mutants not yet killed by the already-selected set
///        length       = number of steps in the candidate (shorter preferred)
///        resetsAdded  = 0 if the candidate's start state matches the current end
///                       state (can be chained without a reset), 1 otherwise
///
///   5. Stops when all killable mutants are covered, or no remaining candidate
///      contributes any new kills.
///
/// Computational cost: O(|testCases| × |mutants|) per call — significantly higher
/// than greedy set-cover, but directly preserves mutation score.
/// </summary>
public class MutationScorePreservingReductionStrategy : IReductionStrategy
{
    public string Name => "MutationScorePreserving";

    private readonly MutationEngine _engine;
    private readonly double _alpha;
    private readonly double _beta;
    private readonly double _gamma;

    /// <param name="engine">
    /// Mutation engine to use; defaults to the standard six-operator engine.
    /// </param>
    /// <param name="alpha">
    /// Weight for unique-kills contribution (primary objective). Default: 1.0.
    /// </param>
    /// <param name="beta">
    /// Penalty weight for test-case length (minimise input length). Default: 0.1.
    /// </param>
    /// <param name="gamma">
    /// Penalty weight for resets (chain-preference objective). Default: 0.1.
    /// </param>
    public MutationScorePreservingReductionStrategy(
        MutationEngine? engine = null,
        double alpha = 1.0,
        double beta = 0.1,
        double gamma = 0.1)
    {
        _engine = engine ?? new MutationEngine();
        _alpha = alpha;
        _beta  = beta;
        _gamma = gamma;
    }

    public TestSuite Reduce(TestSuite suite, FSM fsm)
    {
        if (suite.TestCases.Count == 0)
            return new TestSuite($"{suite.Name}_MutationScorePreserved");

        var mutants = _engine.GenerateMutants(fsm);
        if (mutants.Count == 0)
            return new TestSuite($"{suite.Name}_MutationScorePreserved", suite.TestCases);

        var killMap = BuildKillMap(suite, fsm, mutants);

        var killableMutants = killMap.Values
            .SelectMany(s => s)
            .ToHashSet();

        // If no test case kills any mutant (equivalent mutants only), preserve the suite.
        if (killableMutants.Count == 0)
            return new TestSuite($"{suite.Name}_MutationScorePreserved", suite.TestCases);

        var remaining      = suite.TestCases.ToList();
        var selected       = new List<TestCase>();
        var killedSoFar    = new HashSet<int>();
        var currentEndState = fsm.InitialState;
        int maxLength      = suite.TestCases.Max(tc => tc.Length);

        while (killedSoFar.Count < killableMutants.Count && remaining.Count > 0)
        {
            TestCase? best    = null;
            double bestScore  = double.MinValue;

            foreach (var candidate in remaining)
            {
                int uniqueKills = killMap[candidate.Id].Count(m => !killedSoFar.Contains(m));
                if (uniqueKills == 0) continue;

                var candidateStart = candidate.Steps.Count > 0
                    ? candidate.Steps[0].Transition.From
                    : fsm.InitialState;
                int resetsAdded = candidateStart.Equals(currentEndState) ? 0 : 1;

                double normalizedKills  = (double)uniqueKills / mutants.Count;
                double normalizedLength = maxLength > 1
                    ? (double)candidate.Length / maxLength
                    : 0.0;

                double score = _alpha * normalizedKills
                             - _beta  * normalizedLength
                             - _gamma * resetsAdded;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null) break;

            selected.Add(best);
            killedSoFar.UnionWith(killMap[best.Id]);
            currentEndState = best.Steps.Count > 0
                ? best.Steps[^1].Transition.To
                : fsm.InitialState;
            remaining.Remove(best);
        }

        return new TestSuite($"{suite.Name}_MutationScorePreserved", selected);
    }

    /// <summary>
    /// Executes every test case against every mutant to determine which mutants
    /// each test case kills. Returns a dictionary from test case ID to the set of
    /// mutant indices (0-based into <paramref name="mutants"/>) that it kills.
    /// </summary>
    private Dictionary<string, HashSet<int>> BuildKillMap(
        TestSuite suite, FSM fsm, List<Mutant> mutants)
    {
        var executor = new FSMExecutor();

        var expectedOutputs = suite.TestCases.ToDictionary(
            tc => tc.Id,
            tc => executor.Execute(tc, fsm).Outputs);

        var killMap = suite.TestCases.ToDictionary(
            tc => tc.Id,
            _  => new HashSet<int>());

        for (int i = 0; i < mutants.Count; i++)
        {
            foreach (var tc in suite.TestCases)
            {
                var result = executor.Execute(tc, mutants[i].MutatedFSM);
                if (!result.Outputs.SequenceEqual(expectedOutputs[tc.Id]) || result.HasError)
                    killMap[tc.Id].Add(i);
            }
        }

        return killMap;
    }
}
