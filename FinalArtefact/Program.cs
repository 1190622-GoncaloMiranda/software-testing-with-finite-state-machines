using System.Text.Json;
using FinalArtefact.Configuration;
using FinalArtefact.Pipeline;
using FinalArtefact.Validation;
using FinalArtefact.IO;

namespace FinalArtefact;

/// <summary>
/// FSM Test Suite Generator — Final Artefact
///
/// Generates an optimised, prioritised test suite from a Finite State Machine model,
/// runs mutation testing to evaluate fault detection, and exports:
///   - test_suite.json  : prioritised test cases for external test runners
///   - metrics_report.json : full metrics including pre/post mutation scores
///
/// Usage:
///   dotnet run                          -- uses toolconfig.json in current directory
///   dotnet run -- path/to/config.json   -- uses the specified configuration file
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║            FSM Test Suite Generator — Final Artefact     ║");
        Console.WriteLine("║   Master's Thesis — Gonçalo Miranda, ISEP                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ── 1. Load configuration ──────────────────────────────────────────────

        string configPath = args.Length > 0 ? args[0] : "toolconfig.json";

        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine($"[Error] Configuration file not found: {configPath}");
            Console.Error.WriteLine("  Provide a path as the first argument, or create 'toolconfig.json' in the current directory.");
            return 1;
        }

        ToolConfiguration config;
        try
        {
            var json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<ToolConfiguration>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Config file deserialised to null.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Error] Failed to parse configuration: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[Config] Loaded: {configPath}");
        Console.WriteLine($"  FSM model     : {config.FsmModel}");
        Console.WriteLine($"  Output        : {config.OutputDirectory}");

        // ── 2. Load FSM ────────────────────────────────────────────────────────

        var loader = new FsmJsonLoader();
        FinalArtefact.Core.FSM fsm;
        try
        {
            fsm = loader.Load(config.FsmModel);
            Console.WriteLine($"\n[Loader] {fsm}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Error] Failed to load FSM: {ex.Message}");
            return 1;
        }

        // ── 3. Validate FSM ────────────────────────────────────────────────────

        var validator = new FsmValidator();
        var validation = validator.Validate(fsm, config.Reduction.Type, config.Reduction.FsmSizeThreshold);

        foreach (var warning in validation.Warnings)
            Console.WriteLine($"[Warning] {warning}");

        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                Console.Error.WriteLine($"[Error] {error}");
            return 1;
        }

        // ── 4. Run generation pipeline ─────────────────────────────────────────

        Console.WriteLine("\n[Pipeline] Starting...");
        Console.WriteLine($"  FSM: {fsm.Name}  ({fsm.States.Count} states, {fsm.Transitions.Count} transitions)");

        try
        {
            var pipeline = new TestGenerationPipeline();
            pipeline.Run(fsm, config, validation.Warnings);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Error] Pipeline failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine("\n[Done] Test suite and metrics report written to: " + config.OutputDirectory);
        return 0;
    }
}
