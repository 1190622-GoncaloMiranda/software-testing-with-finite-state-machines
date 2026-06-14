using System.Globalization;
using ExperimentalPrototype.Evaluation;

namespace ExperimentalPrototype.IO;

/// <summary>
/// Exports evaluation results to a CSV file for statistical analysis.
///
/// Output columns (matching thesis experiment design):
///   FSM, CoverageStrategy, ReductionStrategy, PrioritizationStrategy,
///   SuiteSize, TotalSteps, PreReductionTotalSteps, ReductionRatio, ExecutionTimeSavings,
///   StateCoverage, TransitionCoverage,
///   MutationScore, KilledMutants, TotalMutants, APFD, GenerationTimeMs,
///   MS_TransitionTargetMutation, MS_OutputMutation, MS_TransitionRemoval,
///   MS_TransitionAddition, MS_InputMutation, MS_InitialStateMutation
///
/// The six MS_* columns give the per-operator mutation score and allow the
/// reader to judge whether overall MutationScore is driven by discriminating
/// operators (target, output, input, ...) or by operators that are trivially
/// detected (e.g. InitialStateMutation shifts reachability).
/// Empty cells indicate the operator produced no mutants for that FSM.
/// </summary>
public class CsvExporter
{
    private static readonly string Header =
        "FSM,CoverageStrategy,ReductionStrategy,PrioritizationStrategy," +
        "SuiteSize,TotalSteps,PreReductionTotalSteps,ReductionRatio,ExecutionTimeSavings," +
        "StateCoverage,TransitionCoverage," +
        "MutationScore,KilledMutants,TotalMutants,APFD,GenerationTimeMs," +
        "MS_TransitionTargetMutation,MS_OutputMutation,MS_TransitionRemoval," +
        "MS_TransitionAddition,MS_InputMutation,MS_InitialStateMutation";

    /// <summary>
    /// Exports a list of evaluation results to the given CSV file path.
    /// Overwrites any existing file.
    /// </summary>
    public void Export(IEnumerable<EvaluationResult> results, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        using var writer = new StreamWriter(outputPath, append: false);
        writer.WriteLine(Header);

        foreach (var r in results)
            writer.WriteLine(ToRow(r));

        Console.WriteLine($"[CsvExporter] Results written to: {outputPath}");
    }

    /// <summary>
    /// Appends results to an existing CSV (creates file + header if needed).
    /// Useful for incremental experiment runs.
    /// </summary>
    public void Append(IEnumerable<EvaluationResult> results, string outputPath)
    {
        bool fileExists = File.Exists(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        using var writer = new StreamWriter(outputPath, append: true);
        if (!fileExists)
            writer.WriteLine(Header);

        foreach (var r in results)
            writer.WriteLine(ToRow(r));
    }

    private static string ToRow(EvaluationResult r)
    {
        var inv = CultureInfo.InvariantCulture;

        // NaN → empty cell so spreadsheets treat it as missing rather than 0.
        static string Num(double v, string fmt)
            => double.IsNaN(v) ? "" : v.ToString(fmt, CultureInfo.InvariantCulture);

        return string.Join(",",
            Escape(r.FSMName),
            Escape(r.CoverageStrategy),
            Escape(r.ReductionStrategy),
            Escape(r.PrioritizationStrategy),
            r.SuiteSize,
            r.TotalSteps,
            r.PreReductionTotalSteps,
            Num(r.ReductionRatio,        "F4"),
            Num(r.ExecutionTimeSavings,  "F4"),
            r.StateCoverage.ToString("F4", inv),
            r.TransitionCoverage.ToString("F4", inv),
            r.MutationScore.ToString("F4", inv),
            r.KilledMutants,
            r.TotalMutants,
            r.APFD.ToString("F4", inv),
            r.GenerationTimeMs.ToString("F2", inv),
            Num(r.MS_TransitionTargetMutation, "F4"),
            Num(r.MS_OutputMutation,           "F4"),
            Num(r.MS_TransitionRemoval,        "F4"),
            Num(r.MS_TransitionAddition,       "F4"),
            Num(r.MS_InputMutation,            "F4"),
            Num(r.MS_InitialStateMutation,     "F4")
        );
    }

    private static string Escape(string value)
    {
        // Wrap in quotes if the value contains commas or quotes
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
