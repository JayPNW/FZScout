namespace FZScout;

/// <param name="Slug">The name in log files and in <c>/api/servers/{slug}</c> URLs.</param>
/// <param name="Name">The <c>serverName</c> the POST API expects.</param>
record Server(string Slug, string Name, Uri Site)
{
    public static readonly Server Fangbreaker = new("fangbreaker", "Fangbreaker", new("https://fangbreaker.zone"));

    public static readonly Server Frostreaver = new("frostreaver", "Frostreaver", new("https://frostreaver.zone"));

    public static readonly IReadOnlyList<Server> All = [Fangbreaker, Frostreaver];

    /// <summary>Finds the server for the <c>{server}</c> in a log file name, ignoring case. Null for servers we don't report.</summary>
    public static Server? FromSlug(string slug)
    {
        return All.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString() => Name;
}
