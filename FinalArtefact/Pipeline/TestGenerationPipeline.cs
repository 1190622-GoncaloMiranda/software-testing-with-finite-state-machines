using System.Diagnostics;
using FinalArtefact.Configuration;
using FinalArtefact.Core;
using FinalArtefact.Engine;
using FinalArtefact.Mutation;
using FinalArtefact.Output;
using FinalArtefact.Strategies;
using FinalArtefact.Strategies.Coverage;
using FinalArtefact.Strategies.Prioritization;
using FinalArtefact.Strategies.Reduction;
using FinalArtefact.TestModel;
using FinalArtefact.Validation;

namespace FinalArtefact.Pipeline;

/// <summary>
/// Orchestrates the full test generation pipeline for a single FSM and strategy
/// configuration:
///
///   1. Validate FSM (determinism, reachability, size warnings)
///   2. Select strategies from configuration
///   3. Generate raw test suite using the coverage strategy
///   4. Run pre-reduction mutation testing (records baseline mutation score)
///   5. Reduce raw suite using the reduction strategy
///   6. Prioritise reduced suite
///   7. Run post-reduction mutation testing
///   8. Classify mutants (killed / strategy-unreachable / surviving)
///   9. Compute all metrics and build GenerationReport
///  10. Export test_suite.json and metrics_report.json
/// </summary>
public class TestGenerationPipeline
{
    private readonly FsmValidator _validator = new();
    private readonly MutationEngine _mutationEngine = new();
    private readonly MutationTester _mutationTester = new();
    private readonly FSMExecutor _executor = new();
    private readonly TestSuiteExporter _suiteExporter = new();
    private readonly MetricsReportExporter _reportExporter = new();

    public void Run(FSM fsm, ToolConfiguration config, IReadOnlyList<string> validationWarnings)
    {
        var sw = Stopwatch.StartNew();

        // ── 1. Resolve strategies from configuration ───────────────────────────

        var coverage       = ResolveCoverage(config.Coverage);
        var reduction      = ResolveReduction(config.Reduction);
        var prioritisation = ResolvePrioritisation(config.Prioritisation);

        Console.WriteLine($"  Coverage      : {coverage.Name}");
        Console.WriteLine($"  Reduction     : {reduction.Name}");
        Console.WriteLine($"  Prioritisation: {prioritisation.Name}");

        // ── 2. Generate raw suite ──────────────────────────────────────────────

        Console.WriteLine("\n[Pipeline] Generating paths...");
        var paths = coverage.GeneratePaths(fsm);
        var rawTestCases = paths.Select(p => new TestCase(p.Select(t => new TestStep(t)))).ToList();
        var rawSuite = new TestSuite($"{fsm.Name}_raw", rawTestCases);

        int preReductionSize  = rawSuite.Size;
        int preReductionSteps = rawSuite.TotalSteps;
        Console.WriteLine($"  Raw suite: {preReductionSize} test cases, {preReductionSteps} steps");

        // ── 3. Generate mutants ────────────────────────────────────────────────

        Console.WriteLine("\n[Pipeline] Generating mutants...");
        var mutants = _mutationEngine.GenerateMutants(fsm);
        Console.WriteLine($"  Generated {mutants.Count} mutants");

        // ── 4. Pre-reduction mutation testing ─────────────────────────────────

        Console.WriteLine("\n[Pipeline] Running pre-reduction mutation testing...");
        var preResult = _mutationTester.Run(rawSuite, fsm, mutants);
        double preReductionMutationScore = preResult.MutationScore;
        Console.WriteLine($"  Pre-reduction mutation score: {preReductionMutationScore:P1}  ({preResult.KilledMutants}/{preResult.TotalMutants})");

        // Reset kill state before post-reduction run
        foreach (var m in mutants)
            m.IsKilled = false;

        // ── 5. Reduce ──────────────────────────────────────────────────────────

        Console.WriteLine("\n[Pipeline] Applying reduction...");
        var reducedSuite = reduction.Reduce(rawSuite, fsm);
        Console.WriteLine($"  Reduced suite: {reducedSuite.Size} test cases, {reducedSuite.TotalSteps} steps");

        // ── 6. Prioritise ──────────────────────────────────────────────────────

        Console.WriteLine("\n[Pipeline] Applying prioritisation...");
        var finalSuite = prioritisation.Prioritize(reducedSuite, fsm);
        Console.WriteLine($"  Final suite  : {finalSuite.Size} test cases");

        sw.Stop();
        double generationTimeMs = sw.Elapsed.TotalMilliseconds;

        // ── 7. Post-reduction mutation testing ────────────────────────────────

        Console.WriteLine("\n[Pipeline] Running post-reduction mutation testing...");
        var postResult = _mutationTester.Run(finalSuite, fsm, mutants);
        Console.WriteLine($"  Post-reduction mutation score: {postResult.MutationScore:P1}  ({postResult.KilledMutants}/{postResult.TotalMutants})");

        // ── 8. Classify mutants ────────────────────────────────────────────────

        var classification = MutantClassifier.Classify(postResult.Mutants);
        Console.WriteLine($"\n[Pipeline] Mutant classification:");
        Console.WriteLine($"  Killed              : {classification.Killed.Count}");
        Console.WriteLine($"  Strategy-unreachable: {classification.StrategyUnreachable.Count}  (TransitionAddition — excluded from score)");
        Console.WriteLine($"  Surviving           : {classification.Surviving.Count}");
        Console.WriteLine($"  Adjusted score      : {classification.AdjustedMutationScore:P1}");

        // ── 9. Compute metrics ─────────────────────────────────────────────────

        double stateCoverage      = ComputeStateCoverage(finalSuite, fsm);
        double transitionCoverage = ComputeTransitionCoverage(finalSuite, fsm);
        double apfd               = ComputeAPFD(finalSuite, postResult);
        double reductionRatio     = preReductionSize > 0
            ? 1.0 - (double)finalSuite.Size / preReductionSize
            : 0.0;
        double execTimeSavings    = preReductionSteps > 0
            ? 1.0 - (double)finalSuite.TotalSteps / preReductionSteps
            : 0.0;

        var perOpScores = BuildPerOperatorScores(postResult, classification);

        // ── 10. Build report ───────────────────────────────────────────────────

        var report = new GenerationReport
        {
            FsmName     = fsm.Name,
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Configuration = new ReportConfiguration
            {
                CoverageStrategy       = coverage.Name,
                ReductionStrategy      = reduction.Name,
                PrioritisationStrategy = prioritisation.Name
            },
            SuiteMetrics = new SuiteMetrics
            {
                SuiteSize              = finalSuite.Size,
                TotalSteps             = finalSuite.TotalSteps,
                PreReductionSuiteSize  = preReductionSize,
                PreReductionTotalSteps = preReductionSteps,
                ReductionRatio         = Math.Round(reductionRatio,        4),
                ExecutionTimeSavings   = Math.Round(execTimeSavings,       4),
                StateCoverage          = Math.Round(stateCoverage,         4),
                TransitionCoverage     = Math.Round(transitionCoverage,    4),
                Apfd                   = Math.Round(apfd,                  4),
                GenerationTimeMs       = Math.Round(generationTimeMs,      2)
            },
            MutationMetrics = new MutationMetrics
            {
                PreReductionMutationScore  = Math.Round(preReductionMutationScore,             4),
                PostReductionMutationScore = Math.Round(postResult.MutationScore,              4),
                AdjustedMutationScore      = Math.Round(classification.AdjustedMutationScore,  4),
                TotalMutants               = postResult.TotalMutants,
                KilledMutants              = classification.Killed.Count,
                StrategyUnreachableMutants = classification.StrategyUnreachable.Count,
                SurvivingMutants           = classification.Surviving.Count,
                PerOperatorScores          = perOpScores
            },
            Warnings = validationWarnings.ToList()
        };

        // ── 11. Export ─────────────────────────────────────────────────────────

        var outDir = config.OutputDirectory;
        Directory.CreateDirectory(outDir);

        _suiteExporter.Export(
            finalSuite, fsm.Name,
            coverage.Name, reduction.Name, prioritisation.Name,
            Path.Combine(outDir, "test_suite.json"));

        _reportExporter.Export(report, Path.Combine(outDir, "metrics_report.json"));

        Console.WriteLine($"\n[Pipeline] Done in {generationTimeMs:F1} ms.");
    }

    // ── Strategy factories ─────────────────────────────────────────────────────

    private static ICoverageStrategy ResolveCoverage(CoverageConfig cfg) =>
        cfg.Type switch
        {
            "StateCoverage"          => new StateCoverageStrategy(),
            "TransitionCoverage"     => new TransitionCoverageStrategy(),
            "TransitionPairCoverage" => new TransitionPairCoverageStrategy(),
            "SequenceCoverage"       => new SequenceCoverageStrategy(cfg.SequenceLength),
            _ => throw new InvalidOperationException(
                     $"Unknown coverage strategy: '{cfg.Type}'. " +
                     "Valid options: StateCoverage, TransitionCoverage, TransitionPairCoverage, SequenceCoverage.")
        };

    private static IReductionStrategy ResolveReduction(ReductionConfig cfg) =>
        cfg.Type switch
        {
            "DuplicateRemoval"        => new DuplicateRemovalStrategy(),
            "CoveragePreserving"      => new CoveragePreservingReductionStrategy(),
            "SimilarityReduction"     => new SimilarityReductionStrategy(cfg.SimilarityThreshold),
            "MutationScorePreserving" => new MutationScorePreservingReductionStrategy(),
            _ => throw new InvalidOperationException(
                     $"Unknown reduction strategy: '{cfg.Type}'. " +
                     "Valid options: DuplicateRemoval, CoveragePreserving, SimilarityReduction, MutationScorePreserving.")
        };

    private static IPrioritizationStrategy ResolvePrioritisation(PrioritisationConfig cfg) =>
        cfg.Type switch
        {
            "CoverageBased"     => new CoverageBasedPrioritization(),
            "WeightedTransition" => new WeightedTransitionPrioritization(),
            _ => throw new InvalidOperationException(
                     $"Unknown prioritisation strategy: '{cfg.Type}'. " +
                     "Valid options: CoverageBased, WeightedTransition.")
        };

    // ── Metric computations ────────────────────────────────────────────────────

    private static double ComputeStateCoverage(TestSuite suite, FSM fsm)
    {
        var reachable = fsm.GetReachableStates().Select(s => s.Name).ToHashSet();
        if (reachable.Count == 0) return 1.0;
        var visited = suite.GetVisitedStateNames();
        int covered = visited.Count(s => reachable.Contains(s));
        return (double)covered / reachable.Count;
    }

    private static double ComputeTransitionCoverage(TestSuite suite, FSM fsm)
    {
        if (fsm.Transitions.Count == 0) return 1.0;
        var covered = suite.GetCoveredTransitionIds();
        int hit = fsm.Transitions.Count(t => covered.Contains(t.Id));
        return (double)hit / fsm.Transitions.Count;
    }

    /// <summary>
    /// APFD = 1 - Σ TF_i / (n * m) + 1/(2n)
    /// where TF_i is the 1-based position of the first test that kills mutant i,
    /// n is the suite size, and m is the number of mutants killed by the suite.
    /// </summary>
    private double ComputeAPFD(TestSuite suite, MutationTestResult result)
    {
        int n = suite.TestCases.Count;
        if (n == 0 || result.TotalMutants == 0) return 0.0;

        double sumTF = 0;
        int m = 0;

        foreach (var mutant in result.Mutants)
        {
            int firstKill = 0;
            for (int i = 0; i < suite.TestCases.Count; i++)
            {
                var tc = suite.TestCases[i];
                var res = _executor.Execute(tc, mutant.MutatedFSM);
                if (!res.Outputs.SequenceEqual(result.ExpectedOutputs[tc.Id]) || res.HasError)
                {
                    firstKill = i + 1;
                    break;
                }
            }

            if (firstKill > 0)
            {
                sumTF += firstKill;
                m++;
            }
        }

        if (m == 0) return 0.0;
        return 1.0 - (sumTF / ((double)n * m)) + (1.0 / (2.0 * n));
    }

    private static Dictionary<string, double?> BuildPerOperatorScores(
        MutationTestResult result, MutantClassificationResult classification)
    {
        var unreachableIds = classification.StrategyUnreachable
            .Select(m => m.OperatorName)
            .ToHashSet();

        var byOp = result.ScoreByOperator();
        var scores = new Dictionary<string, double?>();

        // Report in a fixed, deterministic operator order
        var operatorOrder = new[]
        {
            "TransitionTargetMutation", "OutputMutation", "TransitionRemoval",
            "TransitionAddition", "InputMutation", "InitialStateMutation"
        };

        foreach (var op in operatorOrder)
        {
            if (!byOp.TryGetValue(op, out var v) || v.Total == 0)
            {
                scores[op] = null;
                continue;
            }

            // For strategy-unreachable operators, report null (not computable)
            if (op == "TransitionAddition" && classification.StrategyUnreachable.Count > 0 &&
                classification.Killed.All(m => m.OperatorName != "TransitionAddition"))
            {
                scores[op] = null;
            }
            else
            {
                scores[op] = v.Total > 0 ? Math.Round((double)v.Killed / v.Total, 4) : (double?)null;
            }
        }

        return scores;
    }
}
