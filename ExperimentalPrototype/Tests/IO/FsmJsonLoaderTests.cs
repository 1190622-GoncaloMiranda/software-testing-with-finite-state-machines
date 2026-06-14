using ExperimentalPrototype.IO;
using NUnit.Framework;

namespace ExperimentalPrototype.Tests.IO;

[TestFixture]
public class FsmJsonLoaderTests
{
    private FsmJsonLoader _loader = null!;

    private const string SimpleJson = """
        {
          "name": "TestFSM",
          "initialState": "S0",
          "states": ["S0", "S1"],
          "transitions": [
            { "from": "S0", "to": "S1", "input": "a", "output": "x" },
            { "from": "S1", "to": "S0", "input": "b", "output": "y" }
          ]
        }
        """;

    [SetUp]
    public void SetUp() => _loader = new FsmJsonLoader();

    [Test]
    public void Parse_ValidJson_LoadsCorrectName()
    {
        Assert.That(_loader.Parse(SimpleJson).Name, Is.EqualTo("TestFSM"));
    }

    [Test]
    public void Parse_ValidJson_LoadsCorrectStateCount()
    {
        Assert.That(_loader.Parse(SimpleJson).States, Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_ValidJson_LoadsCorrectTransitionCount()
    {
        Assert.That(_loader.Parse(SimpleJson).Transitions, Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_ValidJson_SetsCorrectInitialState()
    {
        Assert.That(_loader.Parse(SimpleJson).InitialState.Name, Is.EqualTo("S0"));
    }

    [Test]
    public void Parse_InvalidJson_ThrowsException()
    {
        Assert.That(() => _loader.Parse("{ not valid json"), Throws.Exception);
    }

    [Test]
    public void Load_ExistingBenchmarkFile_ParsesSuccessfully()
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(), "Examples", "mqtt_fsm.json");

        var fsm = _loader.Load(path);

        Assert.That(fsm.States, Is.Not.Empty);
        Assert.That(fsm.Transitions, Is.Not.Empty);
    }
}
