using ExperimentalPrototype.Tests.Helpers;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.TestModel;

[TestFixture]
public class TestSuiteTests
{
    // ── TestCase ──────────────────────────────────────────────────────────────

    [Test]
    public void TestCase_Length_EqualsNumberOfTransitions()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);

        Assert.That(tc.Length, Is.EqualTo(fsm.Transitions.Count));
    }

    [Test]
    public void TestCase_CoveredTransitions_ContainsAllStepTransitions()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);
        var coveredIds = tc.CoveredTransitions.Select(t => t.Id).ToHashSet();

        foreach (var t in fsm.Transitions)
            Assert.That(coveredIds, Does.Contain(t.Id));
    }

    [Test]
    public void TestCase_VisitedStates_ContainsFromAndToStateNames()
    {
        var fsm = FsmFixtures.Linear3();
        var tc = FsmFixtures.MakeTestCase(fsm.Transitions);
        var visitedNames = tc.VisitedStates.Select(s => s.Name).ToHashSet();

        Assert.That(visitedNames, Does.Contain("A"));
        Assert.That(visitedNames, Does.Contain("B"));
        Assert.That(visitedNames, Does.Contain("C"));
    }

    // ── TestSuite ─────────────────────────────────────────────────────────────

    [Test]
    public void TestSuite_Size_EqualsNumberOfTestCases()
    {
        var fsm = FsmFixtures.Linear3();
        var tc1 = FsmFixtures.MakeTestCase(fsm.Transitions.Take(1));
        var tc2 = FsmFixtures.MakeTestCase(fsm.Transitions.Take(2));
        var suite = FsmFixtures.MakeSuite("s", tc1, tc2);

        Assert.That(suite.Size, Is.EqualTo(2));
    }

    [Test]
    public void TestSuite_TotalSteps_IsSumOfAllCaseLengths()
    {
        var fsm = FsmFixtures.Linear3();
        var tc1 = FsmFixtures.MakeTestCase(fsm.Transitions.Take(1)); // 1 step
        var tc2 = FsmFixtures.MakeTestCase(fsm.Transitions.Take(2)); // 2 steps
        var suite = FsmFixtures.MakeSuite("s", tc1, tc2);

        Assert.That(suite.TotalSteps, Is.EqualTo(3));
    }

    [Test]
    public void TestSuite_GetCoveredTransitionIds_IsUnionAcrossAllCases()
    {
        var fsm = FsmFixtures.Linear3();
        var transitions = fsm.Transitions.ToList();
        var tc1 = FsmFixtures.MakeTestCase(new[] { transitions[0] });
        var tc2 = FsmFixtures.MakeTestCase(new[] { transitions[1] });
        var suite = FsmFixtures.MakeSuite("s", tc1, tc2);

        var covered = suite.GetCoveredTransitionIds();
        Assert.That(covered, Does.Contain(transitions[0].Id));
        Assert.That(covered, Does.Contain(transitions[1].Id));
    }

    [Test]
    public void TestSuite_GetVisitedStateNames_IsUnionAcrossAllCases()
    {
        var fsm = FsmFixtures.Linear3();
        var transitions = fsm.Transitions.ToList();
        var tc1 = FsmFixtures.MakeTestCase(new[] { transitions[0] }); // A→B
        var tc2 = FsmFixtures.MakeTestCase(new[] { transitions[1] }); // B→C
        var suite = FsmFixtures.MakeSuite("s", tc1, tc2);

        var visited = suite.GetVisitedStateNames();
        Assert.That(visited, Does.Contain("A"));
        Assert.That(visited, Does.Contain("B"));
        Assert.That(visited, Does.Contain("C"));
    }
}
