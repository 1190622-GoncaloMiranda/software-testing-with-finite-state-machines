using ExperimentalPrototype.Core;
using ExperimentalPrototype.Engine;
using ExperimentalPrototype.TestModel;

namespace ExperimentalPrototype.Mutation;

/// <summary>
/// Runs a test suite against a set of mutants to determine which mutants are killed.
/// A mutant is considered "killed" when at least one test case in the suite produces
/// a different output sequence on the mutant than on the original FSM.
///
/// Mutation Score = Killed Mutants / Total Mutants
/// </summary>
public class MutationTester
{
    private readonly FSMExecutor _executor;

    public MutationTester()
    {
        _executor = new FSMExecutor();
    }

    /// <summary>
    /// Runs every test case in the suite against every mutant.
    /// Marks each mutant as killed or alive and returns the full result.
    /// </summary>
    public MutationTestResult Run(TestSuite suite, FSM originalFsm, List<Mutant> mutants)
    {
        // Pre-compute expected outputs for each test case on the ORIGINAL FSM.
        // These outputs are exposed via MutationTestResult so that downstream
        // metrics (e.g. APFD) can reuse them without re-executing the suite.
        var expectedOutputs = suite.TestCases.ToDictionary(
            tc => tc.Id,
            tc => _executor.Execute(tc, originalFsm).Outputs);

        foreach (var mutant in mutants)
        {
            foreach (var tc in suite.TestCases)
            {
                var mutantResult = _executor.Execute(tc, mutant.MutatedFSM);
                var expected = expectedOutputs[tc.Id];

                // A mutant is killed if any test produces a different output
                if (!mutantResult.Outputs.SequenceEqual(expected) || mutantResult.HasError)
                {
                    mutant.IsKilled = true;
                    break; // No need to run more tests against a killed mutant
                }
            }
        }

        return new MutationTestResult(suite, mutants, expectedOutputs);
    }
}

/// <summary>
/// Holds the outcome of a full mutation testing run.
/// </summary>
public class MutationTestResult
{
    public TestSuite Suite { get; }
    public IReadOnlyList<Mutant> Mutants { get; }

    /// <summary>
    /// Expected output sequences for each test case when executed against the
    /// original FSM. Computed once in MutationTester.Run and exposed here so
    /// that APFD and other metrics can reuse them instead of re-executing.
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> ExpectedOutputs { get; }

    public int TotalMutants => Mutants.Count;
    public int KilledMutants => Mutants.Count(m => m.IsKilled);
    public int AliveMutants => TotalMutants - KilledMutants;

    /// <summary>
    /// Mutation Score: ratio of killed to total mutants.
    /// Returns 1.0 if there are no mutants (trivially perfect).
    /// </summary>
    public double MutationScore =>
        TotalMutants == 0 ? 1.0 : (double)KilledMutants / TotalMutants;

    /// <summary>
    /// Per-operator breakdown: for each operator name, returns (killed, total)
    /// counts restricted to mutants produced by that operator. Operators with
    /// zero generated mutants are omitted. Used by the evaluation layer to
    /// expose where fault detection actually comes from, so that e.g. kills
    /// driven by <c>InitialStateMutation</c> (which shifts reachability and
    /// tends to be trivially detected) can be inspected separately.
    /// </summary>
    public Dictionary<string, (int Killed, int Total)> ScoreByOperator()
    {
        var byOp = new Dictionary<string, (int Killed, int Total)>();
        foreach (var m in Mutants)
        {
            byOp.TryGetValue(m.OperatorName, out var prev);
            byOp[m.OperatorName] = (
                Killed: prev.Killed + (m.IsKilled ? 1 : 0),
                Total:  prev.Total + 1);
        }
        return byOp;
    }

    public MutationTestResult(
        TestSuite suite,
        List<Mutant> mutants,
        IReadOnlyDictionary<string, List<string>> expectedOutputs)
    {
        Suite = suite;
        Mutants = mutants.AsReadOnly();
        ExpectedOutputs = expectedOutputs;
    }

    public void PrintSummary()
    {
        Console.WriteLine($"  Mutation Score : {MutationScore:P1}");
        Console.WriteLine($"  Killed / Total : {KilledMutants} / {TotalMutants}");
        Console.WriteLine($"  Alive mutants  : {AliveMutants}");
    }
}
