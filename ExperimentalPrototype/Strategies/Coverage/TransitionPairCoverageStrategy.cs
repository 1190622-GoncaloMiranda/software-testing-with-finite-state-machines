using ExperimentalPrototype.Core;
using ExperimentalPrototype.Strategies;

namespace ExperimentalPrototype.Strategies.Coverage;

/// <summary>
/// Generates paths that cover all pairs of consecutive transitions (t1, t2)
/// where t1.To == t2.From. This subsumes transition coverage and exposes
/// more interaction faults.
/// Criterion: Transition-Pair Coverage (also called "Switch Coverage").
/// </summary>
public class TransitionPairCoverageStrategy : ICoverageStrategy
{
    public string Name => "TransitionPairCoverage";

    public List<List<Transition>> GeneratePaths(FSM fsm)
    {
        // Build all valid pairs (t1, t2) where t1.To == t2.From
        var pairs = new HashSet<(string, string)>();
        foreach (var t1 in fsm.Transitions)
            foreach (var t2 in fsm.GetOutgoingTransitions(t1.To))
                pairs.Add((t1.Id, t2.Id));

        var coveredPairs = new HashSet<(string, string)>();
        var paths = new List<List<Transition>>();

        while (coveredPairs.Count < pairs.Count)
        {
            // Find an uncovered pair
            var targetPair = pairs.First(p => !coveredPairs.Contains(p));
            var t1 = fsm.Transitions.First(t => t.Id == targetPair.Item1);
            var t2 = fsm.Transitions.First(t => t.Id == targetPair.Item2);

            // Build path: initial -> t1.From -> t1 -> t2
            var path = new List<Transition>();

            var prefix = BfsShortestPath(fsm, fsm.InitialState, t1.From);
            if (prefix != null) path.AddRange(prefix);

            path.Add(t1);
            path.Add(t2);

            // Register all pairs covered by this path
            for (int i = 0; i + 1 < path.Count; i++)
                coveredPairs.Add((path[i].Id, path[i + 1].Id));

            paths.Add(path);

            // Safety: break if no new pair was covered (unreachable pair)
            if (!coveredPairs.Contains(targetPair))
                coveredPairs.Add(targetPair); // skip unreachable pair
        }

        return paths;
    }

    private List<Transition>? BfsShortestPath(FSM fsm, State source, State target)
    {
        if (source.Equals(target)) return new List<Transition>();

        var visited = new HashSet<string> { source.Name };
        var queue = new Queue<(State, List<Transition>)>();
        queue.Enqueue((source, new List<Transition>()));

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();
            foreach (var t in fsm.GetOutgoingTransitions(current))
            {
                if (!visited.Add(t.To.Name)) continue;
                var newPath = new List<Transition>(path) { t };
                if (t.To.Equals(target)) return newPath;
                queue.Enqueue((t.To, newPath));
            }
        }
        return null;
    }
}
