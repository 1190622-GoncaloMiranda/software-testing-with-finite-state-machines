using ExperimentalPrototype.Core;

namespace ExperimentalPrototype.Strategies.Coverage;

/// <summary>
/// Generates all transition sequences of length up to MaxLength starting from
/// the initial state, allowing state repetition (cycles). Redundant paths that
/// are strict prefixes of longer paths are pruned, since the longer path already
/// contains the shorter one as a contiguous subsequence.
///
/// Implements the consecutive-sequence coverage criterion (Σ*wΣ*) from
/// Elyasaf et al. [32]: for every k-length consecutive sequence of transitions w
/// reachable from the initial state, the suite contains at least one test case in
/// which w appears as a contiguous subsequence.
///
/// Criterion: k-sequence coverage for k = MaxLength.
/// </summary>
public class SequenceCoverageStrategy : ICoverageStrategy
{
    public int MaxLength { get; }
    public string Name => $"SequenceCoverage(maxLen={MaxLength})";

    public SequenceCoverageStrategy(int maxLength = 4)
    {
        if (maxLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "MaxLength must be >= 1");
        MaxLength = maxLength;
    }

    public List<List<Transition>> GeneratePaths(FSM fsm)
    {
        var allPaths = new List<List<Transition>>();
        DFS(fsm, fsm.InitialState, new List<Transition>(), allPaths);
        return FilterRedundantPaths(allPaths);
    }

    private void DFS(
        FSM fsm,
        State current,
        List<Transition> currentPath,
        List<List<Transition>> result)
    {
        if (currentPath.Count > 0)
            result.Add(new List<Transition>(currentPath));

        if (currentPath.Count >= MaxLength)
            return;

        foreach (var t in fsm.GetOutgoingTransitions(current))
        {
            currentPath.Add(t);
            DFS(fsm, t.To, currentPath, result);
            currentPath.RemoveAt(currentPath.Count - 1);
        }
    }

    /// <summary>
    /// Removes paths that are strict prefixes of other paths in the list,
    /// since the longer path already subsumes the shorter one's coverage.
    /// </summary>
    private List<List<Transition>> FilterRedundantPaths(List<List<Transition>> paths)
    {
        var sorted = paths.OrderByDescending(p => p.Count).ToList();
        var result = new List<List<Transition>>();

        foreach (var path in sorted)
        {
            bool isSubsumed = result.Any(existing => IsPrefix(path, existing));
            if (!isSubsumed)
                result.Add(path);
        }

        return result;
    }

    private bool IsPrefix(List<Transition> shorter, List<Transition> longer)
    {
        if (shorter.Count > longer.Count) return false;
        for (int i = 0; i < shorter.Count; i++)
            if (!shorter[i].Equals(longer[i])) return false;
        return true;
    }
}
