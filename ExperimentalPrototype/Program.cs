using ExperimentalPrototype.Evaluation;
using ExperimentalPrototype.IO;
using ExperimentalPrototype.Strategies.Coverage;
using ExperimentalPrototype.Strategies.Reduction;
using ExperimentalPrototype.Strategies.Prioritization;

namespace ExperimentalPrototype;

/// <summary>
/// Entry point for the FSM-Based Automated Test Generation Framework.
///
/// Demonstrates the complete experimental pipeline:
///   1. Load FSMs from JSON
///   2. Define strategy combinations
///   3. Run full factorial experiment (generate, reduce, prioritize, mutate, evaluate)
///   4. Export results to CSV
///
/// Usage:
///   dotnet run                          -- runs with bundled example FSMs
///   dotnet run -- path/to/my_fsm.json  -- runs with a custom FSM file
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ExperimentalPrototype                   ║");
        Console.WriteLine("║   Master's Thesis Prototype — Gonçalo Miranda, ISEP      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ── 1. Load FSMs ─────────────────────────────────────────────────────
        var loader = new FsmJsonLoader();
        var fsms = new List<Core.FSM>();

        if (args.Length > 0)
        {
            // Load user-supplied FSM file(s)
            foreach (var path in args)
            {
                try
                {
                    var fsm = loader.Load(path);
                    fsms.Add(fsm);
                    Console.WriteLine($"[Loader] Loaded: {fsm}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Loader] Failed to load '{path}': {ex.Message}");
                }
            }
        }
        else
        {
            // Default: load all bundled example FSMs
            var examplesDir = Path.Combine(AppContext.BaseDirectory, "Examples");

            // Fallback for running from project root (dotnet run)
            if (!Directory.Exists(examplesDir))
                examplesDir = Path.Combine(Directory.GetCurrentDirectory(), "Examples");

            foreach (var file in Directory.EnumerateFiles(examplesDir, "*.json"))
            {
                try
                {
                    var fsm = loader.Load(file);
                    fsms.Add(fsm);
                    Console.WriteLine($"[Loader] Loaded: {fsm}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Loader] Skipped '{file}': {ex.Message}");
                }
            }
        }

        if (fsms.Count == 0)
        {
            Console.Error.WriteLine("[Error] No FSMs loaded. Exiting.");
            return;
        }

        // ── 2. Define Strategy Pools ─────────────────────────────────────────

        var coverageStrategies = new List<Strategies.ICoverageStrategy>
        {
            new StateCoverageStrategy(),
            new TransitionCoverageStrategy(),
            new TransitionPairCoverageStrategy(),
            new SequenceCoverageStrategy(maxLength: 3)
        };

        var reductionStrategies = new List<Strategies.IReductionStrategy>
        {
            new DuplicateRemovalStrategy(),
            new CoveragePreservingReductionStrategy(),
            new SimilarityReductionStrategy(similarityThreshold: 0.8),
            new MutationScorePreservingReductionStrategy()
        };

        var prioritizationStrategies = new List<Strategies.IPrioritizationStrategy>
        {
            new CoverageBasedPrioritization(),
            new WeightedTransitionPrioritization()
        };

        // ── 3. Run Experiments ────────────────────────────────────────────────

        var outputPath = Path.Combine(
            Directory.GetCurrentDirectory(), "Results", "experiment_results.csv");

        Console.WriteLine($"\n[Experiment] Starting full factorial experiment...");
        Console.WriteLine($"  FSMs               : {fsms.Count}");
        Console.WriteLine($"  Coverage strategies: {coverageStrategies.Count}");
        Console.WriteLine($"  Reduction strategies: {reductionStrategies.Count}");
        Console.WriteLine($"  Prioritization     : {prioritizationStrategies.Count}");
        int totalCombinations = fsms.Count
            * coverageStrategies.Count
            * reductionStrategies.Count
            * prioritizationStrategies.Count;
        Console.WriteLine($"  Total combinations : {totalCombinations}");

        var runner = new ExperimentRunner();
        runner.RunAndExport(
            fsms,
            coverageStrategies,
            reductionStrategies,
            prioritizationStrategies,
            outputPath,
            verbose: true);

        Console.WriteLine("\n[Done] Experiment complete.");
        Console.WriteLine($"       Results: {outputPath}");
    }
}
