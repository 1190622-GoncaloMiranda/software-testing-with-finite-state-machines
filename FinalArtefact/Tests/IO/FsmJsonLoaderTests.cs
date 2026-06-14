using FinalArtefact.IO;
using NUnit.Framework;

namespace FinalArtefact.Tests.IO;

[TestFixture]
public class FsmJsonLoaderTests
{
    private readonly FsmJsonLoader _loader = new();

    private const string LinearJson = """
        {
          "name": "TCP",
          "initialState": "S0",
          "states": ["S0", "S1", "S2"],
          "transitions": [
            { "from": "S0", "to": "S1", "input": "open",  "output": "ok"    },
            { "from": "S1", "to": "S2", "input": "close", "output": "closed" }
          ]
        }
        """;

    // ── Parse: happy path ──────────────────────────────────────────────────────

    [Test]
    public void Parse_ValidJson_SetsFsmName()
    {
        var fsm = _loader.Parse(LinearJson);

        Assert.That(fsm.Name, Is.EqualTo("TCP"));
    }

    [Test]
    public void Parse_ValidJson_SetsInitialState()
    {
        var fsm = _loader.Parse(LinearJson);

        Assert.That(fsm.InitialState.Name, Is.EqualTo("S0"));
    }

    [Test]
    public void Parse_ValidJson_LoadsAllStates()
    {
        var fsm = _loader.Parse(LinearJson);

        Assert.That(fsm.States.Select(s => s.Name),
            Is.EquivalentTo(new[] { "S0", "S1", "S2" }));
    }

    [Test]
    public void Parse_ValidJson_LoadsAllTransitions()
    {
        var fsm = _loader.Parse(LinearJson);

        Assert.That(fsm.Transitions, Has.Count.EqualTo(2));
        Assert.That(fsm.Transitions.Select(t => t.Input),
            Is.EquivalentTo(new[] { "open", "close" }));
    }

    [Test]
    public void Parse_ValidJson_TransitionOutputsCorrect()
    {
        var fsm = _loader.Parse(LinearJson);
        var t = fsm.Transitions.Single(t => t.Input == "open");

        Assert.That(t.Output, Is.EqualTo("ok"));
        Assert.That(t.From.Name, Is.EqualTo("S0"));
        Assert.That(t.To.Name, Is.EqualTo("S1"));
    }

    // ── Parse: edge / defensive cases ─────────────────────────────────────────

    [Test]
    public void Parse_MissingNameField_UsesDefaultName()
    {
        const string json = """
            { "initialState": "S0", "states": ["S0"], "transitions": [] }
            """;

        var fsm = _loader.Parse(json, "Fallback");

        Assert.That(fsm.Name, Is.EqualTo("Fallback"));
    }

    [Test]
    public void Parse_StatesNotListed_AutoCreatedFromTransitions()
    {
        // states array is absent — From/To in transitions should create states
        const string json = """
            {
              "name": "Auto",
              "initialState": "A",
              "transitions": [
                { "from": "A", "to": "B", "input": "x", "output": "y" }
              ]
            }
            """;

        var fsm = _loader.Parse(json);

        Assert.That(fsm.States.Select(s => s.Name), Is.EquivalentTo(new[] { "A", "B" }));
    }

    [Test]
    public void Parse_MissingInitialState_ThrowsInvalidOperationException()
    {
        const string json = """
            { "states": ["S0"], "transitions": [] }
            """;

        Assert.Throws<InvalidOperationException>(() => _loader.Parse(json));
    }

    [Test]
    public void Parse_TrailingCommasAndComments_ParsesWithoutError()
    {
        const string json = """
            {
              "name": "Lenient",
              "initialState": "S0",
              "states": ["S0",],
              "transitions": [],
            }
            """;

        Assert.DoesNotThrow(() => _loader.Parse(json));
    }

    [Test]
    public void Parse_CaseInsensitiveKeys_ParsesCorrectly()
    {
        const string json = """
            { "Name": "X", "InitialState": "S0", "States": ["S0"], "Transitions": [] }
            """;

        var fsm = _loader.Parse(json);

        Assert.That(fsm.Name, Is.EqualTo("X"));
        Assert.That(fsm.InitialState.Name, Is.EqualTo("S0"));
    }

    // ── Load: file I/O ─────────────────────────────────────────────────────────

    [Test]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _loader.Load("/nonexistent/path/fsm.json"));
    }

    [Test]
    public void Load_ExistingFile_ParsesNameFromContents()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, LinearJson);

            var fsm = _loader.Load(path);

            Assert.That(fsm.Name, Is.EqualTo("TCP"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_FileWithoutNameField_UsesStemAsDefaultName()
    {
        var path = Path.Combine(Path.GetTempPath(), "my_fsm.json");
        try
        {
            File.WriteAllText(path, """
                { "initialState": "S0", "states": ["S0"], "transitions": [] }
                """);

            var fsm = _loader.Load(path);

            Assert.That(fsm.Name, Is.EqualTo("my_fsm"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
