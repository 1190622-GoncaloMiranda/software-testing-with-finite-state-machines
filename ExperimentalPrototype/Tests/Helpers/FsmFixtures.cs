using ExperimentalPrototype.Core;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Tests.Helpers;

/// <summary>
/// Reusable FSM fixtures and builder helpers for unit tests.
/// </summary>
internal static class FsmFixtures
{
    /// <summary>
    /// A 3-state cyclic FSM: A -[connect/ack]-> B -[send/ok]-> C -[reset/done]-> A
    /// All 3 states and 3 transitions are reachable.
    /// </summary>
    public static FSM Linear3()
    {
        var sA = new State("A");
        var sB = new State("B");
        var sC = new State("C");
        return new FSM(sA, new[] { sA, sB, sC }, new[]
        {
            new Transition(sA, sB, "connect", "ack"),
            new Transition(sB, sC, "send",    "ok"),
            new Transition(sC, sA, "reset",   "done")
        }) { Name = "Linear3" };
    }

    /// <summary>
    /// A 4-state branching FSM:
    ///   Init -[go_left/going_left]->  Left  -[done/finished]-> End
    ///   Init -[go_right/going_right]-> Right -[done/finished]-> End
    /// </summary>
    public static FSM Branching4()
    {
        var sInit  = new State("Init");
        var sLeft  = new State("Left");
        var sRight = new State("Right");
        var sEnd   = new State("End");
        return new FSM(sInit, new[] { sInit, sLeft, sRight, sEnd }, new[]
        {
            new Transition(sInit,  sLeft,  "go_left",  "going_left"),
            new Transition(sInit,  sRight, "go_right", "going_right"),
            new Transition(sLeft,  sEnd,   "done",     "finished"),
            new Transition(sRight, sEnd,   "done",     "finished")
        }) { Name = "Branching4" };
    }

    /// <summary>
    /// A minimal 2-state toggle FSM: S0 -[a/x]-> S1 -[b/y]-> S0
    /// Useful for mutation testing where exact counts are easy to reason about.
    /// </summary>
    public static FSM Toggle2()
    {
        var s0 = new State("S0");
        var s1 = new State("S1");
        return new FSM(s0, new[] { s0, s1 }, new[]
        {
            new Transition(s0, s1, "a", "x"),
            new Transition(s1, s0, "b", "y")
        }) { Name = "Toggle2" };
    }

    /// <summary>
    /// Builds a TestCase from a sequence of transitions (from the same FSM).
    /// </summary>
    public static TestCase MakeTestCase(IEnumerable<Transition> transitions)
        => new TestCase(transitions.Select(t => new TestStep(t)));

    /// <summary>
    /// Builds a TestSuite containing the given test cases.
    /// </summary>
    public static TestSuite MakeSuite(string name, params TestCase[] cases)
        => new TestSuite(name, cases);

    /// <summary>
    /// Converts coverage paths to a TestSuite.
    /// </summary>
    public static TestSuite PathsToSuite(string name, List<List<Transition>> paths)
        => new TestSuite(name, paths.Select(p => MakeTestCase(p)));
}
