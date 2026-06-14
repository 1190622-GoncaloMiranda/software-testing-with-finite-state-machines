using System.Text.Json;
using System.Text.Json.Serialization;
using ExperimentalPrototype.Core;

namespace ExperimentalPrototype.IO;

/// <summary>
/// Loads an FSM from a JSON file.
///
/// Expected JSON format:
/// {
///   "name": "MyFSM",                         (optional)
///   "initialState": "S0",
///   "states": ["S0", "S1", "S2"],
///   "transitions": [
///     { "from": "S0", "to": "S1", "input": "login",   "output": "ok"      },
///     { "from": "S1", "to": "S2", "input": "confirm",  "output": "success" }
///   ]
/// }
/// </summary>
public class FsmJsonLoader
{
    public FSM Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"FSM JSON file not found: {filePath}");

        string json = File.ReadAllText(filePath);
        return Parse(json, Path.GetFileNameWithoutExtension(filePath));
    }

    public FSM Parse(string json, string defaultName = "FSM")
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var dto = JsonSerializer.Deserialize<FsmDto>(json, options)
                  ?? throw new InvalidOperationException("Failed to deserialise FSM JSON.");

        // Build State objects
        var stateMap = (dto.States ?? Array.Empty<string>())
            .Distinct()
            .ToDictionary(name => name, name => new State(name));

        // Resolve initial state
        if (string.IsNullOrWhiteSpace(dto.InitialState))
            throw new InvalidOperationException("FSM JSON must specify 'initialState'.");

        if (!stateMap.TryGetValue(dto.InitialState, out var initialState))
        {
            // Auto-add initial state if missing from the states list
            initialState = new State(dto.InitialState);
            stateMap[dto.InitialState] = initialState;
        }

        // Build Transition objects
        var transitions = new List<Transition>();
        foreach (var td in dto.Transitions ?? Array.Empty<TransitionDto>())
        {
            if (!stateMap.TryGetValue(td.From, out var from))
                stateMap[td.From] = from = new State(td.From);
            if (!stateMap.TryGetValue(td.To, out var to))
                stateMap[td.To] = to = new State(td.To);

            transitions.Add(new Transition(from, to, td.Input, td.Output));
        }

        return new FSM(initialState, stateMap.Values, transitions)
        {
            Name = string.IsNullOrWhiteSpace(dto.Name) ? defaultName : dto.Name
        };
    }

    // ── DTO types used only for deserialisation ────────────────────────────

    private class FsmDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("initialState")]
        public string InitialState { get; set; } = "";

        [JsonPropertyName("states")]
        public string[]? States { get; set; }

        [JsonPropertyName("transitions")]
        public TransitionDto[]? Transitions { get; set; }
    }

    private class TransitionDto
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = "";

        [JsonPropertyName("to")]
        public string To { get; set; } = "";

        [JsonPropertyName("input")]
        public string Input { get; set; } = "";

        [JsonPropertyName("output")]
        public string Output { get; set; } = "";
    }
}
