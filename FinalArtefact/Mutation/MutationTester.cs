using FinalArtefact.Core;
using FinalArtefact.Engine;
using FinalArtefact.TestModel;

namespace FinalArtefact.Mutation;

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

    public MutationTestResult Run(TestSuite suite, FSM originalFsm, List<Mutant> mutants)
    {
        var expectedOutputs = suite.TestCases.ToDictionary(
            tc => tc.Id,
            tc => _executor.Execute(tc, originalFsm).Outputs);

        foreach (var mutant in mutants)
        {
            foreach (var tc in suite.TestCases)
            {
                var mutantResult = _executor.Execute(tc, mutant.MutatedFSM);
                var expected = expectedOutputs[tc.Id];

                if (!mutantResult.Outputs.SequenceEqual(expected) || mutantResult.HasError)
                {
                    mutant.IsKilled = true;
                    break;
                }
            }
        }

        return new MutationTestResult(suite, mutants, expectedOutputs);
    }
}

public class MutationTestResult
{
    public TestSuite Suite { get; }
    public IReadOnlyList<Mutant> Mutants { get; }
    public IReadOnlyDictionary<string, List<string>> ExpectedOutputs { get; }

    public int TotalMutants => Mutants.Count;
    public int KilledMutants => Mutants.Count(m => m.IsKilled);
    public int AliveMutants => TotalMutants - KilledMutants;

    public double MutationScore =>
        TotalMutants == 0 ? 1.0 : (double)KilledMutants / TotalMutants;

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
}
