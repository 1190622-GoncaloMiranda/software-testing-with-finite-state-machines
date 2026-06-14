using System.Text.Json.Serialization;

namespace FinalArtefact.Configuration;

/// <summary>
/// Root configuration model. Deserialised from a JSON config file supplied
/// at launch. All strategy selections and tuneable parameters are declared here.
/// </summary>
public class ToolConfiguration
{
    [JsonPropertyName("fsmModel")]
    public string FsmModel { get; set; } = "";

    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = "output/";

    [JsonPropertyName("coverage")]
    public CoverageConfig Coverage { get; set; } = new();

    [JsonPropertyName("reduction")]
    public ReductionConfig Reduction { get; set; } = new();

    [JsonPropertyName("prioritisation")]
    public PrioritisationConfig Prioritisation { get; set; } = new();
}

public class CoverageConfig
{
    /// <summary>
    /// One of: StateCoverage, TransitionCoverage, TransitionPairCoverage, SequenceCoverage.
    /// Default: TransitionPairCoverage (strongest and most consistent per experimental results).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "TransitionPairCoverage";

    /// <summary>
    /// Maximum sequence length k for SequenceCoverage. Ignored for other criteria.
    /// </summary>
    [JsonPropertyName("sequenceLength")]
    public int SequenceLength { get; set; } = 3;
}

public class ReductionConfig
{
    /// <summary>
    /// One of: DuplicateRemoval, CoveragePreserving, SimilarityReduction, MutationScorePreserving.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "MutationScorePreserving";

    /// <summary>
    /// Number of FSM transitions above which a warning is emitted when
    /// MutationScorePreserving reduction is selected (it is O(tests × mutants)).
    /// The reduction still runs — the warning prompts the user to reconsider.
    /// </summary>
    [JsonPropertyName("fsmSizeThreshold")]
    public int FsmSizeThreshold { get; set; } = 50;

    /// <summary>
    /// Normalised BMI similarity threshold for SimilarityReduction. Ignored for other strategies.
    /// </summary>
    [JsonPropertyName("similarityThreshold")]
    public double SimilarityThreshold { get; set; } = 0.8;
}

public class PrioritisationConfig
{
    /// <summary>
    /// One of: CoverageBased, WeightedTransition.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "CoverageBased";
}
