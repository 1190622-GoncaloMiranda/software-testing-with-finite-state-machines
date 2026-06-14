using ExperimentalPrototype.Core;
using ExperimentalPrototype.Strategies;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Engine;

/// <summary>
/// Orchestrates the full test suite generation pipeline:
///   1. Generate paths from the FSM using a coverage strategy
///   2. Convert each path into a TestCase
///   3. Apply reduction to remove redundant test cases
///   4. Apply prioritization to reorder test cases for early fault detection
///
/// All strategy dependencies are injected, making the engine fully composable.
/// </summary>
public class TestGenerator
{
    private readonly ICoverageStrategy _coverage;
    private readonly IReductionStrategy _reduction;
    private readonly IPrioritizationStrategy _prioritization;

    public TestGenerator(
        ICoverageStrategy coverage,
        IReductionStrategy reduction,
        IPrioritizationStrategy prioritization)
    {
        _coverage = coverage;
        _reduction = reduction;
        _prioritization = prioritization;
    }

    /// <summary>
    /// Runs the complete generation pipeline and returns a ready-to-execute test suite.
    /// </summary>
    /// <param name="fsm">The FSM model to generate tests for.</param>
    /// <param name="fsmName">Label used in the suite name (for reporting).</param>
    /// <returns>Optimised, prioritised TestSuite plus pre-reduction size/steps for ratio computation.</returns>
    public (TestSuite suite, TimeSpan generationTime, int preReductionSize, int preReductionSteps) Generate(FSM fsm, string? fsmName = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Generate paths according to the coverage criterion
        var paths = _coverage.GeneratePaths(fsm);

        // Step 2: Convert paths to test cases
        var testCases = paths.Select(path => PathToTestCase(path)).ToList();

        var rawSuite = new TestSuite(
            $"{fsmName ?? fsm.Name}_{_coverage.Name}_raw",
            testCases);

        // Capture pre-reduction baseline for reduction ratio and execution-time savings
        int preReductionSize  = rawSuite.Size;
        int preReductionSteps = rawSuite.TotalSteps;

        // Step 3: Reduce
        var reducedSuite = _reduction.Reduce(rawSuite, fsm);

        // Step 4: Prioritize
        var finalSuite = _prioritization.Prioritize(reducedSuite, fsm);
        finalSuite.Name = BuildSuiteName(fsmName ?? fsm.Name);

        sw.Stop();
        return (finalSuite, sw.Elapsed, preReductionSize, preReductionSteps);
    }

    /// <summary>
    /// Converts a raw transition path into a TestCase.
    /// </summary>
    private TestCase PathToTestCase(List<Transition> path)
    {
        var steps = path.Select(t => new TestStep(t));
        return new TestCase(steps);
    }

    private string BuildSuiteName(string fsmName) =>
        $"{fsmName}|{_coverage.Name}|{_reduction.Name}|{_prioritization.Name}";
}
