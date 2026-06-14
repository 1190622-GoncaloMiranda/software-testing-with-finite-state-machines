using ExperimentalPrototype.Core;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Engine;

/// <summary>
/// Executes test cases against an FSM model by simulating state transitions.
/// The executor tracks the current state and fires each step's transition,
/// collecting the output sequence for verdict determination.
/// </summary>
public class FSMExecutor
{
    private const string ErrorToken = "ERROR";

    /// <summary>
    /// Executes a single test case against the given FSM.
    /// Returns the observed output sequence (one entry per step).
    /// Steps that encounter a missing transition produce "ERROR".
    /// </summary>
    public ExecutionResult Execute(TestCase testCase, FSM fsm)
    {
        var outputs = new List<string>();
        var currentState = fsm.InitialState;
        bool hasError = false;

        foreach (var step in testCase.Steps)
        {
            // Look up the transition from the current state on the step's input
            var transition = fsm.GetTransition(currentState, step.Transition.Input);

            if (transition == null)
            {
                // No transition defined: fault or specification gap
                outputs.Add(ErrorToken);
                hasError = true;
                break; // Cannot continue after an undefined transition
            }

            outputs.Add(transition.Output);
            currentState = transition.To;
        }

        return new ExecutionResult(testCase, outputs, hasError, currentState);
    }

    /// <summary>
    /// Executes an entire test suite and returns all results.
    /// </summary>
    public List<ExecutionResult> ExecuteSuite(TestSuite suite, FSM fsm)
    {
        return suite.TestCases.Select(tc => Execute(tc, fsm)).ToList();
    }

    /// <summary>
    /// Compares expected outputs (from step transitions) against actual outputs.
    /// Returns true if the test case passes (all outputs match and no error).
    /// </summary>
    public static bool IsPass(ExecutionResult result)
    {
        if (result.HasError) return false;

        var expected = result.TestCase.Steps.Select(s => s.Transition.Output).ToList();
        if (expected.Count != result.Outputs.Count) return false;

        return expected.SequenceEqual(result.Outputs);
    }
}

/// <summary>
/// Holds the complete result of executing one TestCase against one FSM.
/// </summary>
public record ExecutionResult(
    TestCase TestCase,
    List<string> Outputs,
    bool HasError,
    State FinalState);
