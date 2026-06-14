using FinalArtefact.Core;
using FinalArtefact.TestModel;

namespace FinalArtefact.Strategies;

public interface ICoverageStrategy
{
    string Name { get; }
    List<List<Transition>> GeneratePaths(FSM fsm);
}

public interface IReductionStrategy
{
    string Name { get; }
    TestSuite Reduce(TestSuite suite, FSM fsm);
}

public interface IPrioritizationStrategy
{
    string Name { get; }
    TestSuite Prioritize(TestSuite suite, FSM fsm);
}
