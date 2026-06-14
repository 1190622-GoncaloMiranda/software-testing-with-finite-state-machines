using ExperimentalPrototype.Core;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Strategies;

/// <summary>
/// Strategy for generating FSM paths that satisfy a given coverage criterion.
/// Each implementation explores the FSM graph differently (BFS, DFS, pair coverage, etc.).
/// </summary>
public interface ICoverageStrategy
{
    string Name { get; }

    /// <summary>
    /// Generates a list of transition paths through the FSM.
    /// Each path is a sequence of transitions starting from the initial state.
    /// </summary>
    List<List<Transition>> GeneratePaths(FSM fsm);
}

/// <summary>
/// Strategy for reducing a test suite by removing redundant or low-value test cases
/// while attempting to preserve coverage and fault-detection potential.
/// </summary>
public interface IReductionStrategy
{
    string Name { get; }

    /// <summary>
    /// Returns a reduced version of the given test suite.
    /// The original suite is not modified.
    /// </summary>
    TestSuite Reduce(TestSuite suite, FSM fsm);
}

/// <summary>
/// Strategy for reordering test cases within a suite to maximise early fault detection.
/// </summary>
public interface IPrioritizationStrategy
{
    string Name { get; }

    /// <summary>
    /// Returns a new test suite with test cases reordered according to the strategy.
    /// The original suite is not modified.
    /// </summary>
    TestSuite Prioritize(TestSuite suite, FSM fsm);
}
