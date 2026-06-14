using FinalArtefact.Core;
using FinalArtefact.TestModel;

namespace FinalArtefact.Engine;

/// <summary>
/// Executes test cases against an FSM model by simulating state transitions.
/// The executor tracks the current state and fires each step's transition,
/// collecting the output sequence for verdict determination.
/// </summary>
public class FSMExecutor
{
    private const string ErrorToken = "ERROR";

    public ExecutionResult Execute(TestCase testCase, FSM fsm)
    {
        var outputs = new List<string>();
        var currentState = fsm.InitialState;
        bool hasError = false;

        foreach (var step in testCase.Steps)
        {
            var transition = fsm.GetTransition(currentState, step.Transition.Input);

            if (transition == null)
            {
                outputs.Add(ErrorToken);
                hasError = true;
                break;
            }

            outputs.Add(transition.Output);
            currentState = transition.To;
        }

        return new ExecutionResult(testCase, outputs, hasError, currentState);
    }

    public List<ExecutionResult> ExecuteSuite(TestSuite suite, FSM fsm)
    {
        return suite.TestCases.Select(tc => Execute(tc, fsm)).ToList();
    }

    public static bool IsPass(ExecutionResult result)
    {
        if (result.HasError) return false;

        var expected = result.TestCase.Steps.Select(s => s.Transition.Output).ToList();
        if (expected.Count != result.Outputs.Count) return false;

        return expected.SequenceEqual(result.Outputs);
    }
}

public record ExecutionResult(
    TestCase TestCase,
    List<string> Outputs,
    bool HasError,
    State FinalState);
