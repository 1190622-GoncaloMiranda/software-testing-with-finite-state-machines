using System.Text.Json.Serialization;

namespace FinalArtefact.Output;

public class GenerationReport
{
    [JsonPropertyName("fsmName")]       public string FsmName { get; set; } = "";
    [JsonPropertyName("generatedAt")]   public string GeneratedAt { get; set; } = "";
    [JsonPropertyName("configuration")] public ReportConfiguration Configuration { get; set; } = new();
    [JsonPropertyName("suiteMetrics")]  public SuiteMetrics SuiteMetrics { get; set; } = new();
    [JsonPropertyName("mutationMetrics")] public MutationMetrics MutationMetrics { get; set; } = new();
    [JsonPropertyName("warnings")]      public List<string> Warnings { get; set; } = new();
}

public class ReportConfiguration
{
    [JsonPropertyName("coverageStrategy")]       public string CoverageStrategy { get; set; } = "";
    [JsonPropertyName("reductionStrategy")]      public string ReductionStrategy { get; set; } = "";
    [JsonPropertyName("prioritisationStrategy")] public string PrioritisationStrategy { get; set; } = "";
}

public class SuiteMetrics
{
    [JsonPropertyName("suiteSize")]              public int    SuiteSize { get; set; }
    [JsonPropertyName("totalSteps")]             public int    TotalSteps { get; set; }
    [JsonPropertyName("preReductionSuiteSize")]  public int    PreReductionSuiteSize { get; set; }
    [JsonPropertyName("preReductionTotalSteps")] public int    PreReductionTotalSteps { get; set; }
    [JsonPropertyName("reductionRatio")]         public double ReductionRatio { get; set; }
    [JsonPropertyName("executionTimeSavings")]   public double ExecutionTimeSavings { get; set; }
    [JsonPropertyName("stateCoverage")]          public double StateCoverage { get; set; }
    [JsonPropertyName("transitionCoverage")]     public double TransitionCoverage { get; set; }
    [JsonPropertyName("apfd")]                   public double Apfd { get; set; }
    [JsonPropertyName("generationTimeMs")]       public double GenerationTimeMs { get; set; }
}

public class MutationMetrics
{
    [JsonPropertyName("preReductionMutationScore")]  public double PreReductionMutationScore { get; set; }
    [JsonPropertyName("postReductionMutationScore")] public double PostReductionMutationScore { get; set; }
    [JsonPropertyName("adjustedMutationScore")]      public double AdjustedMutationScore { get; set; }
    [JsonPropertyName("totalMutants")]               public int    TotalMutants { get; set; }
    [JsonPropertyName("killedMutants")]              public int    KilledMutants { get; set; }
    [JsonPropertyName("strategyUnreachableMutants")] public int    StrategyUnreachableMutants { get; set; }
    [JsonPropertyName("survivingMutants")]           public int    SurvivingMutants { get; set; }
    [JsonPropertyName("perOperatorScores")]          public Dictionary<string, double?> PerOperatorScores { get; set; } = new();
}
