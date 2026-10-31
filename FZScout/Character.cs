namespace FZScout;

/// <summary>One character on one server: one <c>eqlog_{Name}_{server}.txt</c>.</summary>
record Character(string Name, Server Server)
{
    public override string ToString() => $"{Name} ({Server})";
}
