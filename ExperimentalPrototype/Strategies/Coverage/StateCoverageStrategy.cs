using ExperimentalPrototype.Core;

namespace ExperimentalPrototype.Strategies.Coverage;

/// <summary>
/// Generates the minimum set of paths (via BFS) that ensures every reachable
/// state is visited at least once. Each state is the target of at least one path.
/// Criterion: State Coverage.
/// </summary>
public class StateCoverageStrategy : ICoverageStrategy
{
    public string Name => "StateCoverage";

    public List<List<Transition>> GeneratePaths(FSM fsm)
    {
        var paths = new List<List<Transition>>();
        var covered = new HashSet<string> { fsm.InitialState.Name };

        // BFS to find shortest path to each uncovered state
        foreach (var targetState in fsm.GetReachableStates())
        {
            if (covered.Contains(targetState.Name) && targetState.Name != fsm.InitialState.Name)
                continue;

            var path = BfsShortestPath(fsm, fsm.InitialState, targetState);
            if (path != null && path.Count > 0)
            {
                paths.Add(path);
                // Mark all states visited by this path as covered
                foreach (var t in path)
                {
                    covered.Add(t.From.Name);
                    covered.Add(t.To.Name);
                }
            }
        }

        // If no paths generated (trivial FSM), generate one per reachable state
        if (paths.Count == 0 && fsm.States.Count > 0)
        {
            foreach (var state in fsm.GetReachableStates().Skip(1))
            {
                var path = BfsShortestPath(fsm, fsm.InitialState, state);
                if (path != null && path.Count > 0)
                    paths.Add(path);
            }
        }

        return paths;
    }

    /// <summary>BFS to find the shortest transition path from source to target.</summary>
    private List<Transition>? BfsShortestPath(FSM fsm, State source, State target)
    {
        if (source.Equals(target))
            return new List<Transition>();

        var visited = new HashSet<string> { source.Name };
        // Queue holds (current state, path so far)
        var queue = new Queue<(State state, List<Transition> path)>();
        queue.Enqueue((source, new List<Transition>()));

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();
            foreach (var transition in fsm.GetOutgoingTransitions(current))
            {
                if (!visited.Add(transition.To.Name))
                    continue;

                var newPath = new List<Transition>(path) { transition };
                if (transition.To.Equals(target))
                    return newPath;

                queue.Enqueue((transition.To, newPath));
            }
        }

        return null; // target not reachable
    }
}
