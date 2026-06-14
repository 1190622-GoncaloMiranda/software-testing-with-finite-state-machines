using ExperimentalPrototype.Core;

namespace ExperimentalPrototype.TestModel;

/// <summary>
/// A collection of test cases forming an executable test suite.
/// Provides aggregate metrics used by the evaluation module.
/// </summary>
public class TestSuite
{
    public string Name { get; set; }
    public List<TestCase> TestCases { get; set; }

    public int Size => TestCases.Count;
    public int TotalSteps => TestCases.Sum(tc => tc.Length);

    public TestSuite(string name, IEnumerable<TestCase>? cases = null)
    {
        Name = name;
        TestCases = cases?.ToList() ?? new List<TestCase>();
    }

    /// <summary>Set of all unique transitions covered by this suite.</summary>
    public HashSet<string> GetCoveredTransitionIds() =>
        TestCases.SelectMany(tc => tc.CoveredTransitions)
                 .Select(t => t.Id)
                 .ToHashSet();

    /// <summary>Set of all unique state names visited by this suite.</summary>
    public HashSet<string> GetVisitedStateNames() =>
        TestCases.SelectMany(tc => tc.VisitedStates)
                 .Select(s => s.Name)
                 .ToHashSet();

    public void Add(TestCase tc) => TestCases.Add(tc);

    public TestSuite Clone() =>
        new TestSuite(Name + "_copy", TestCases.Select(tc => tc.Clone()));

    public override string ToString() =>
        $"TestSuite '{Name}': {Size} test cases, {TotalSteps} total steps";
}
