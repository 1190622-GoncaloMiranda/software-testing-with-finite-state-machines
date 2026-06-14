using FinalArtefact.Core;

namespace FinalArtefact.Mutation;

/// <summary>
/// Represents a single mutant: a modified FSM produced by one mutation operator,
/// tagged with the operator's name and a human-readable description of the change.
/// </summary>
public class Mutant
{
    public FSM MutatedFSM { get; }
    public string OperatorName { get; }
    public string Description { get; }
    public bool IsKilled { get; set; } = false;

    public Mutant(FSM mutatedFSM, string operatorName, string description)
    {
        MutatedFSM = mutatedFSM ?? throw new ArgumentNullException(nameof(mutatedFSM));
        OperatorName = operatorName ?? throw new ArgumentNullException(nameof(operatorName));
        Description = description ?? string.Empty;
    }

    public override string ToString() =>
        $"Mutant [{(IsKilled ? "KILLED" : "ALIVE")}] ({OperatorName}): {Description}";
}
