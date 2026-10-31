namespace FZScout.Logs;

/// <summary>Log files are named <c>eqlog_{CharacterName}_{server}.txt</c>.</summary>
static class LogFileName
{
    public const string Pattern = "eqlog_*.txt";

    /// <summary>The character a log belongs to, or null if it isn't a log for a server we report.</summary>
    public static Character? TryParse(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var parts = name.Split('_');
        if (parts.Length != 3 || !string.Equals(parts[0], "eqlog", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var server = Server.FromSlug(parts[2]);
        if (server is null)
        {
            return null;
        }

        return new Character(parts[1], server);
    }
}
