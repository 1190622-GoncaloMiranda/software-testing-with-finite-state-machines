using FinalArtefact.Core;
using FinalArtefact.Strategies;
using FinalArtefact.TestModel;

namespace FinalArtefact.Engine;

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

    public (TestSuite suite, TimeSpan generationTime, int preReductionSize, int preReductionSteps) Generate(FSM fsm, string? fsmName = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var paths = _coverage.GeneratePaths(fsm);
        var testCases = paths.Select(path => PathToTestCase(path)).ToList();

        var rawSuite = new TestSuite(
            $"{fsmName ?? fsm.Name}_{_coverage.Name}_raw",
            testCases);

        int preReductionSize  = rawSuite.Size;
        int preReductionSteps = rawSuite.TotalSteps;

        var reducedSuite = _reduction.Reduce(rawSuite, fsm);
        var finalSuite = _prioritization.Prioritize(reducedSuite, fsm);
        finalSuite.Name = BuildSuiteName(fsmName ?? fsm.Name);

        sw.Stop();
        return (finalSuite, sw.Elapsed, preReductionSize, preReductionSteps);
    }

    private TestCase PathToTestCase(List<Transition> path)
    {
        var steps = path.Select(t => new TestStep(t));
        return new TestCase(steps);
    }

    private string BuildSuiteName(string fsmName) =>
        $"{fsmName}|{_coverage.Name}|{_reduction.Name}|{_prioritization.Name}";
}
