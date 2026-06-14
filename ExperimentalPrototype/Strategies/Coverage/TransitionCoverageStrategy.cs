using ExperimentalPrototype.Core;
using ExperimentalPrototype.Strategies;

namespace ExperimentalPrototype.Strategies.Coverage;

/// <summary>
/// Generates one test path per FSM transition: the shortest path from the initial
/// state to the transition's source (BFS), followed by the transition itself.
/// Produces exactly |transitions| paths before reduction.
///
/// Implements the all-transitions (AE) criterion: every reachable transition must
/// appear in at least one test case. This also subsumes state coverage.
/// </summary>
public class TransitionCoverageStrategy : ICoverageStrategy
{
    public string Name => "TransitionCoverage";

    public List<List<Transition>> GeneratePaths(FSM fsm)
    {
        var paths = new List<List<Transition>>();

        foreach (var t in fsm.Transitions)
        {
            var prefix = BfsShortestPath(fsm, fsm.InitialState, t.From);
            if (prefix == null) continue; // transition unreachable from initial state

            var path = new List<Transition>(prefix) { t };
            paths.Add(path);
        }

        return paths;
    }

    private static List<Transition>? BfsShortestPath(FSM fsm, State source, State target)
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
