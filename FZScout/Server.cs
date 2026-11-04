namespace FZScout;

record Server(string Name, Uri Site)
{
    public static readonly Server Fangbreaker = new("Fangbreaker", new("https://fangbreaker.zone"));

    public static readonly Server Frostreaver = new("Frostreaver", new("https://frostreaver.zone"));

    public static readonly IReadOnlyList<Server> All = [Fangbreaker, Frostreaver];

    /// <summary>Finds the server for the <c>{server}</c> in a log file name. Null for servers we don't report.</summary>
    public static Server? FromName(string name)
    {
        return All.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString() => Name;
}
