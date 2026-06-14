using System.Text.Json;
using System.Text.Json.Serialization;
using FinalArtefact.TestModel;

namespace FinalArtefact.Output;

public class TestSuiteExporter
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Export(
        TestSuite suite,
        string fsmName,
        string coverageStrategy,
        string reductionStrategy,
        string prioritisationStrategy,
        string outputPath)
    {
        var dto = new TestSuiteDto
        {
            Metadata = new TestSuiteMetadata
            {
                FsmName                = fsmName,
                GeneratedAt            = DateTime.UtcNow.ToString("o"),
                CoverageStrategy       = coverageStrategy,
                ReductionStrategy      = reductionStrategy,
                PrioritisationStrategy = prioritisationStrategy,
                TotalTestCases         = suite.Size
            },
            TestCases = suite.TestCases.Select((tc, index) => new TestCaseDto
            {
                Id       = tc.Id,
                Priority = index + 1,
                Steps    = tc.Steps.Select(s => new TestStepDto
                {
                    FromState      = s.Transition.From.Name,
                    Input          = s.Transition.Input,
                    ExpectedOutput = s.Transition.Output,
                    ToState        = s.Transition.To.Name
                }).ToList()
            }).ToList()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllText(outputPath, JsonSerializer.Serialize(dto, _options));
        Console.WriteLine($"[TestSuiteExporter] {suite.Size} test cases written to: {outputPath}");
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private class TestSuiteDto
    {
        [JsonPropertyName("metadata")]  public TestSuiteMetadata Metadata { get; set; } = new();
        [JsonPropertyName("testCases")] public List<TestCaseDto> TestCases { get; set; } = new();
    }

    private class TestSuiteMetadata
    {
        [JsonPropertyName("fsmName")]                public string FsmName { get; set; } = "";
        [JsonPropertyName("generatedAt")]            public string GeneratedAt { get; set; } = "";
        [JsonPropertyName("coverageStrategy")]       public string CoverageStrategy { get; set; } = "";
        [JsonPropertyName("reductionStrategy")]      public string ReductionStrategy { get; set; } = "";
        [JsonPropertyName("prioritisationStrategy")] public string PrioritisationStrategy { get; set; } = "";
        [JsonPropertyName("totalTestCases")]         public int    TotalTestCases { get; set; }
    }

    private class TestCaseDto
    {
        [JsonPropertyName("id")]       public string Id { get; set; } = "";
        [JsonPropertyName("priority")] public int Priority { get; set; }
        [JsonPropertyName("steps")]    public List<TestStepDto> Steps { get; set; } = new();
    }

    private class TestStepDto
    {
        [JsonPropertyName("fromState")]      public string FromState { get; set; } = "";
        [JsonPropertyName("input")]          public string Input { get; set; } = "";
        [JsonPropertyName("expectedOutput")] public string ExpectedOutput { get; set; } = "";
        [JsonPropertyName("toState")]        public string ToState { get; set; } = "";
    }
}
