namespace FinalArtefact.Core;

/// <summary>
/// Represents a complete Finite State Machine (FSM) with states, transitions,
/// and an initial state. Provides graph traversal helpers used by coverage strategies.
///
/// FSMs are assumed to be deterministic Mealy machines with a (possibly partial)
/// transition function: for any (state, input) pair there is at most one outgoing
/// transition, each transition produces exactly one output symbol, and missing
/// transitions are treated as undefined behaviour at execution time.
/// </summary>
public class FSM
{
    public string Name { get; set; } = "UnnamedFSM";
    public State InitialState { get; }
    public IReadOnlyList<State> States { get; }
    public IReadOnlyList<Transition> Transitions { get; }

    private readonly Dictionary<string, List<Transition>> _adjacency;

    public FSM(State initialState, IEnumerable<State> states, IEnumerable<Transition> transitions)
    {
        InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        States = states.ToList();
        Transitions = transitions.ToList();

        _adjacency = new Dictionary<string, List<Transition>>();
        foreach (var s in States)
            _adjacency[s.Name] = new List<Transition>();

        foreach (var t in Transitions)
        {
            if (!_adjacency.ContainsKey(t.From.Name))
                _adjacency[t.From.Name] = new List<Transition>();
            _adjacency[t.From.Name].Add(t);
        }
    }

    public IReadOnlyList<Transition> GetOutgoingTransitions(State state)
    {
        if (_adjacency.TryGetValue(state.Name, out var list))
            return list;
        return Array.Empty<Transition>();
    }

    public IReadOnlyList<Transition> GetOutgoingTransitions(string stateName)
    {
        if (_adjacency.TryGetValue(stateName, out var list))
            return list;
        return Array.Empty<Transition>();
    }

    public Transition? GetTransition(State from, string input)
    {
        return GetOutgoingTransitions(from)
               .FirstOrDefault(t => t.Input == input);
    }

    public IEnumerable<State> GetReachableStates()
    {
        var visited = new HashSet<string>();
        var queue = new Queue<State>();
        queue.Enqueue(InitialState);
        visited.Add(InitialState.Name);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;
            foreach (var t in GetOutgoingTransitions(current))
            {
                if (visited.Add(t.To.Name))
                    queue.Enqueue(t.To);
            }
        }
    }

    public FSM Clone()
    {
        var stateMap = States.ToDictionary(s => s.Name, s => new State(s.Name));
        var newInitial = stateMap[InitialState.Name];
        var newTransitions = Transitions.Select(t =>
            new Transition(stateMap[t.From.Name], stateMap[t.To.Name], t.Input, t.Output));
        return new FSM(newInitial, stateMap.Values, newTransitions) { Name = Name };
    }

    public FSM WithTransitions(IEnumerable<Transition> newTransitions)
    {
        var stateMap = States.ToDictionary(s => s.Name, s => new State(s.Name));
        var newInitial = stateMap[InitialState.Name];
        var remapped = newTransitions.Select(t =>
            new Transition(stateMap[t.From.Name], stateMap[t.To.Name], t.Input, t.Output));
        return new FSM(newInitial, stateMap.Values, remapped) { Name = Name };
    }

    public override string ToString() =>
        $"FSM '{Name}': {States.Count} states, {Transitions.Count} transitions, initial={InitialState}";
}
