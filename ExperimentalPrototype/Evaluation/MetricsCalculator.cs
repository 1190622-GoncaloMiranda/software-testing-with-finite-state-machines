using ExperimentalPrototype.Core;
using ExperimentalPrototype.Mutation;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Evaluation;

/// <summary>
/// Computes all quantitative evaluation metrics for a test suite and its
/// mutation testing results. Metrics align with those identified in the
/// literature review (thesis Chapter 3, RQ2):
///
///   - State Coverage
///   - Transition Coverage
///   - Test Suite Size
///   - Generation Time
///   - Mutation Score
///   - APFD (Average Percentage of Faults Detected)
/// </summary>
public class MetricsCalculator
{
    // ── Coverage ─────────────────────────────────────────────────────────────

    /// <summary>
    /// State coverage: fraction of reachable states visited by the suite.
    /// </summary>
    public double ComputeStateCoverage(TestSuite suite, FSM fsm)
    {
        var reachable = fsm.GetReachableStates().Select(s => s.Name).ToHashSet();
        if (reachable.Count == 0) return 1.0;

        var visited = suite.GetVisitedStateNames();
        int covered = visited.Count(s => reachable.Contains(s));
        return (double)covered / reachable.Count;
    }

    /// <summary>
    /// Transition coverage: fraction of FSM transitions fired at least once.
    /// </summary>
    public double ComputeTransitionCoverage(TestSuite suite, FSM fsm)
    {
        if (fsm.Transitions.Count == 0) return 1.0;

        var covered = suite.GetCoveredTransitionIds();
        int hit = fsm.Transitions.Count(t => covered.Contains(t.Id));
        return (double)hit / fsm.Transitions.Count;
    }

    // ── APFD ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Average Percentage of Faults Detected (APFD).
    /// Measures how quickly the prioritised suite detects faults (killed mutants).
    ///
    /// Formula: APFD = 1 - (Σ TF_i / (n * m)) + 1/(2n)
    ///   where TF_i = position of first test case in the current suite that kills mutant i,
    ///         n    = number of test cases in the current (possibly reduced) suite,
    ///         m    = number of mutants killed by at least one test case in the current suite.
    ///
    /// All mutants are re-executed against the current suite to determine m and TF_i.
    /// This means reduction strategies that remove the only test case killing a mutant will
    /// lower m and APFD, making the cost of reduction directly visible in the metric.
    ///
    /// Returns 0.0 if the suite is empty or no mutants are killed by the current suite.
    /// Reference: Elbaum et al. (2002); Rechtberger et al. (2022) [thesis ref 17].
    /// </summary>
    /// <param name="expectedOutputs">
    /// Output sequences for each test case when executed against the original FSM.
    /// Typically sourced from <see cref="MutationTestResult.ExpectedOutputs"/> to
    /// avoid recomputing the same executions done during mutation testing.
    /// </param>
    public double ComputeAPFD(
        TestSuite suite,
        List<Mutant> mutants,
        IReadOnlyDictionary<string, List<string>> expectedOutputs)
    {
        int n = suite.TestCases.Count;
        if (n == 0 || mutants.Count == 0) return 0.0;

        var executor = new Engine.FSMExecutor();
        double sumTF = 0;
        int m = 0;

        foreach (var mutant in mutants)
        {
            // Find the 1-based index of the first test case in the current suite that kills this mutant
            int firstKillPosition = 0;
            for (int i = 0; i < suite.TestCases.Count; i++)
            {
                var tc = suite.TestCases[i];
                var result = executor.Execute(tc, mutant.MutatedFSM);
                if (!result.Outputs.SequenceEqual(expectedOutputs[tc.Id]) || result.HasError)
                {
                    firstKillPosition = i + 1; // 1-based
                    break;
                }
            }

            if (firstKillPosition > 0)
            {
                sumTF += firstKillPosition;
                m++;
            }
        }

        if (m == 0) return 0.0;
        return 1.0 - (sumTF / ((double)n * m)) + (1.0 / (2.0 * n));
    }

    // ── Aggregate ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes all metrics and returns a structured result record.
    /// </summary>
    public EvaluationResult Evaluate(
        TestSuite suite,
        FSM fsm,
        MutationTestResult mutationResult,
        TimeSpan generationTime,
        int preReductionSize,
        int preReductionSteps)
    {
        var byOp = mutationResult.ScoreByOperator();

        double ScoreFor(string op) =>
            byOp.TryGetValue(op, out var v) && v.Total > 0
                ? (double)v.Killed / v.Total
                : double.NaN;

        return new EvaluationResult
        {
            SuiteName             = suite.Name,
            SuiteSize             = suite.Size,
            TotalSteps            = suite.TotalSteps,
            PreReductionSuiteSize  = preReductionSize,
            PreReductionTotalSteps = preReductionSteps,
            StateCoverage    = ComputeStateCoverage(suite, fsm),
            TransitionCoverage = ComputeTransitionCoverage(suite, fsm),
            MutationScore    = mutationResult.MutationScore,
            KilledMutants    = mutationResult.KilledMutants,
            TotalMutants     = mutationResult.TotalMutants,
            APFD             = ComputeAPFD(
                                   suite,
                                   mutationResult.Mutants.ToList(),
                                   mutationResult.ExpectedOutputs),
            GenerationTimeMs = generationTime.TotalMilliseconds,

            MS_TransitionTargetMutation = ScoreFor("TransitionTargetMutation"),
            MS_OutputMutation           = ScoreFor("OutputMutation"),
            MS_TransitionRemoval        = ScoreFor("TransitionRemoval"),
            MS_TransitionAddition       = ScoreFor("TransitionAddition"),
            MS_InputMutation            = ScoreFor("InputMutation"),
            MS_InitialStateMutation     = ScoreFor("InitialStateMutation"),
        };
    }
}

/// <summary>
/// Structured container for all evaluation metrics for one experiment run.
/// Serialised to CSV by the CsvExporter.
/// </summary>
public class EvaluationResult
{
    public string SuiteName          { get; set; } = "";

    // Parse suite name into its constituent strategy labels
    public string FSMName            => SuiteName.Split('|').ElementAtOrDefault(0) ?? "";
    public string CoverageStrategy   => SuiteName.Split('|').ElementAtOrDefault(1) ?? "";
    public string ReductionStrategy  => SuiteName.Split('|').ElementAtOrDefault(2) ?? "";
    public string PrioritizationStrategy => SuiteName.Split('|').ElementAtOrDefault(3) ?? "";

    public int    SuiteSize               { get; set; }
    public int    TotalSteps              { get; set; }
    public int    PreReductionSuiteSize   { get; set; }
    public int    PreReductionTotalSteps  { get; set; }

    // Fraction of test cases eliminated by reduction (0 = no reduction, 1 = all removed)
    public double ReductionRatio =>
        PreReductionSuiteSize > 0
            ? 1.0 - (double)SuiteSize / PreReductionSuiteSize
            : double.NaN;

    // Fraction of transition steps eliminated by reduction
    public double ExecutionTimeSavings =>
        PreReductionTotalSteps > 0
            ? 1.0 - (double)TotalSteps / PreReductionTotalSteps
            : double.NaN;

    public double StateCoverage      { get; set; }
    public double TransitionCoverage { get; set; }
    public double MutationScore      { get; set; }
    public int    KilledMutants      { get; set; }
    public int    TotalMutants       { get; set; }
    public double APFD               { get; set; }
    public double GenerationTimeMs   { get; set; }

    // Per-operator mutation scores. NaN means that operator produced no mutants
    // for this FSM — e.g. InputMutation / OutputMutation produce nothing when
    // the FSM has fewer than two distinct input / output symbols.
    public double MS_TransitionTargetMutation { get; set; } = double.NaN;
    public double MS_OutputMutation           { get; set; } = double.NaN;
    public double MS_TransitionRemoval        { get; set; } = double.NaN;
    public double MS_TransitionAddition       { get; set; } = double.NaN;
    public double MS_InputMutation            { get; set; } = double.NaN;
    public double MS_InitialStateMutation     { get; set; } = double.NaN;

    public void Print()
    {
        Console.WriteLine($"  Suite            : {SuiteName}");
        Console.WriteLine($"  Size             : {SuiteSize} test cases / {TotalSteps} steps");
        Console.WriteLine($"  State Coverage   : {StateCoverage:P1}");
        Console.WriteLine($"  Trans. Coverage  : {TransitionCoverage:P1}");
        Console.WriteLine($"  Mutation Score   : {MutationScore:P1}  ({KilledMutants}/{TotalMutants} killed)");
        Console.WriteLine($"  APFD             : {APFD:F4}");
        Console.WriteLine($"  Generation Time  : {GenerationTimeMs:F1} ms");
        Console.WriteLine($"  Per-operator MS  : "
            + $"target={Fmt(MS_TransitionTargetMutation)} "
            + $"output={Fmt(MS_OutputMutation)} "
            + $"remove={Fmt(MS_TransitionRemoval)} "
            + $"add={Fmt(MS_TransitionAddition)} "
            + $"input={Fmt(MS_InputMutation)} "
            + $"initial={Fmt(MS_InitialStateMutation)}");
    }

    private static string Fmt(double v) =>
        double.IsNaN(v) ? "—" : v.ToString("P0");
}
