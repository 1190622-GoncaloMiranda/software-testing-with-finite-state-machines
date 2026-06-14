using ExperimentalPrototype.Core;
using ExperimentalPrototype.Engine;
using ExperimentalPrototype.Evaluation;
using ExperimentalPrototype.IO;
using ExperimentalPrototype.Mutation;
using ExperimentalPrototype.Strategies;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Evaluation;

/// <summary>
/// Orchestrates a full factorial experiment across all strategy combinations
/// for a given FSM (or list of FSMs). For each combination it:
///   1. Generates a test suite
///   2. Runs mutation testing
///   3. Computes all metrics
///   4. Collects results for CSV export
///
/// This is the top-level experimental harness described in the thesis DIMEI phase.
/// </summary>
public class ExperimentRunner
{
    private readonly MutationEngine _mutationEngine;
    private readonly MutationTester _mutationTester;
    private readonly MetricsCalculator _metrics;
    private readonly CsvExporter _exporter;

    public ExperimentRunner()
    {
        _mutationEngine = new MutationEngine();
        _mutationTester = new MutationTester();
        _metrics        = new MetricsCalculator();
        _exporter       = new CsvExporter();
    }

    /// <summary>
    /// Runs experiments for a single FSM across all provided strategy combinations.
    /// Returns all collected EvaluationResult objects.
    /// </summary>
    public List<EvaluationResult> RunExperiments(
        FSM fsm,
        IEnumerable<ICoverageStrategy> coverageStrategies,
        IEnumerable<IReductionStrategy> reductionStrategies,
        IEnumerable<IPrioritizationStrategy> prioritizationStrategies,
        bool verbose = true)
    {
        var results = new List<EvaluationResult>();

        // Pre-generate mutants once per FSM (expensive — do not repeat per combination).
        // Before each combination, IsKilled is reset to false so the same mutant
        // instances can be reused across runs without re-materialising the FSMs.
        if (verbose) Console.WriteLine($"\n[Experiment] Generating mutants for '{fsm.Name}'...");
        var mutants = _mutationEngine.GenerateMutants(fsm);
        if (verbose) Console.WriteLine($"  Generated {mutants.Count} mutants.");

        // Full factorial: all combinations of strategies
        foreach (var cov in coverageStrategies)
        foreach (var red in reductionStrategies)
        foreach (var pri in prioritizationStrategies)
        {
            if (verbose)
                Console.WriteLine($"\n  [{fsm.Name}] {cov.Name} | {red.Name} | {pri.Name}");

            try
            {
                var generator = new TestGenerator(cov, red, pri);
                var (suite, genTime, preSize, preSteps) = generator.Generate(fsm, fsm.Name);

                // Reset kill state on the shared mutant set.
                foreach (var mutant in mutants)
                    mutant.IsKilled = false;

                var mutResult = _mutationTester.Run(suite, fsm, mutants);
                var eval = _metrics.Evaluate(suite, fsm, mutResult, genTime, preSize, preSteps);

                if (verbose) eval.Print();

                results.Add(eval);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"  [ERROR] Combination failed: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Runs experiments across multiple FSMs and exports combined results to CSV.
    /// </summary>
    public void RunAndExport(
        IEnumerable<FSM> fsms,
        IEnumerable<ICoverageStrategy> coverageStrategies,
        IEnumerable<IReductionStrategy> reductionStrategies,
        IEnumerable<IPrioritizationStrategy> prioritizationStrategies,
        string outputCsvPath,
        bool verbose = true)
    {
        var allResults = new List<EvaluationResult>();

        foreach (var fsm in fsms)
        {
            var results = RunExperiments(
                fsm,
                coverageStrategies,
                reductionStrategies,
                prioritizationStrategies,
                verbose);

            allResults.AddRange(results);
        }

        _exporter.Export(allResults, outputCsvPath);
        Console.WriteLine($"\n[Experiment] Done. {allResults.Count} rows exported to '{outputCsvPath}'.");
    }
}
