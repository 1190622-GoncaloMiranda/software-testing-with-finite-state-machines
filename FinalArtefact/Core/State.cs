namespace FinalArtefact.Core;

public class State
{
    public string Name { get; }

    public State(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("State name cannot be null or empty.", nameof(name));
        Name = name;
    }

    public override string ToString() => Name;

    public override bool Equals(object? obj) =>
        obj is State other && Name == other.Name;

    public override int GetHashCode() => Name.GetHashCode();
}
