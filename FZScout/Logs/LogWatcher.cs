namespace FZScout.Logs;

/// <summary>
/// Watches the logs of running EverQuest processes and turns new lines into events.
/// See "Finding logs" and "Logging hint" in InternalDocumentation/SPEC.md.
/// </summary>
class LogWatcher(ConsoleReporter log)
{
    public static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan EverquestLogNotGrowingDelay = TimeSpan.FromMinutes(5);

    public const string EverquestLogNotGrowingMessage = "No EverQuest log is growing. Type `/log on` in EverQuest.";

    /// <summary>Every log for a server we report, in a folder EverQuest is running from, keyed by path.</summary>
    readonly Dictionary<string, WatchedLog> _logs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The folders EverQuest was running from at the last scan.</summary>
    readonly HashSet<string> _folders = new(StringComparer.OrdinalIgnoreCase);

    readonly Dictionary<int, ProcessWatch> _processes = [];

    /// <summary>The logs that have grown, which is how we know a character is playing.</summary>
    public IReadOnlyList<string> ActiveLogs => [.. this._logs.Values.Where(l => l.IsActive).Select(l => l.Tail.Path).Order()];

    /// <summary>Every log being watched for growth, active or not.</summary>
    public IReadOnlyList<string> WatchedLogs => [.. this._logs.Keys.Order()];

    /// <summary>Call every <see cref="ScanInterval"/> with the running EverQuest processes.</summary>
    /// <returns>The characters whose logs are no longer watched because EverQuest stopped.</returns>
    public IReadOnlyList<Character> Scan(IReadOnlyCollection<EqProcess> processes, DateTimeOffset now)
    {
        var folders = processes.Select(p => p.LogsFolder).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stopped = StopWatchingLogsOutside(folders);
        foreach (var folder in folders)
        {
            StartWatchingLogsIn(folder);
        }

        this._folders.Clear();
        this._folders.UnionWith(folders);

        CheckLoggingIsOn(processes, now);
        return stopped;
    }

    /// <summary>Call often. Reads what's been added to each log since the last poll.</summary>
    public IReadOnlyList<CharacterLogEvent> Poll()
    {
        var events = new List<CharacterLogEvent>();
        foreach (var watched in this._logs.Values)
        {
            var lines = ReadNewLines(watched.Tail);
            if (lines.Count == 0)
            {
                continue;
            }

            if (!watched.IsActive)
            {
                watched.IsActive = true;
                log.Info($"Watching {watched.Tail.Path}");
            }

            events.AddRange(lines.Select(LogEvent.Parse).OfType<LogEvent>().Select(e => new CharacterLogEvent(watched.Character, e)));
        }

        return events;
    }

    static IReadOnlyList<string> ReadNewLines(LogTail tail)
    {
        try
        {
            return tail.ReadNewLines();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Busy or gone for the moment; try again next poll.
            return [];
        }
    }

    static Dictionary<string, long> LogSizes(string folder)
    {
        try
        {
            return new DirectoryInfo(folder).EnumerateFiles(LogFileName.Pattern).ToDictionary(f => f.FullName, f => f.Length, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    List<Character> StopWatchingLogsOutside(HashSet<string> folders)
    {
        var stopped = this._logs.Where(l => !folders.Contains(Path.GetDirectoryName(l.Key)!)).ToList();
        foreach (var (path, watched) in stopped)
        {
            this._logs.Remove(path);
            if (watched.IsActive)
            {
                log.Info($"Stopped watching {path}");
            }
        }

        return [.. stopped.Select(l => l.Value.Character)];
    }

    void StartWatchingLogsIn(string folder)
    {
        // A log created while we were already watching the folder is read from its start, so its first lines aren't lost.
        var alreadyWatching = this._folders.Contains(folder);
        foreach (var path in LogSizes(folder).Keys)
        {
            if (this._logs.ContainsKey(path))
            {
                continue;
            }

            var character = LogFileName.TryParse(path);
            if (character is not null)
            {
                this._logs[path] = new WatchedLog(character, new LogTail(path, fromStart: alreadyWatching));
            }
        }
    }

    /// <summary>Any growing log means logging is on, even a log for a server we don't report.</summary>
    void CheckLoggingIsOn(IReadOnlyCollection<EqProcess> processes, DateTimeOffset now)
    {
        var running = processes.Select(p => p.Id).ToHashSet();
        foreach (var gone in this._processes.Keys.Where(id => !running.Contains(id)).ToList())
        {
            this._processes.Remove(gone);
        }

        foreach (var process in processes)
        {
            if (!this._processes.TryGetValue(process.Id, out var watch))
            {
                this._processes[process.Id] = new ProcessWatch(now, LogSizes(process.LogsFolder));
                continue;
            }

            if (watch.Settled)
            {
                continue;
            }

            if (HasGrown(watch.SizesWhenFirstSeen, LogSizes(process.LogsFolder)))
            {
                watch.Settled = true;
            }
            else if (now - watch.FirstSeen >= EverquestLogNotGrowingDelay)
            {
                watch.Settled = true;
                log.Warning(EverquestLogNotGrowingMessage);
            }
        }
    }

    static bool HasGrown(Dictionary<string, long> before, Dictionary<string, long> after)
    {
        return after.Any(file => file.Value > before.GetValueOrDefault(file.Key));
    }

    record WatchedLog(Character Character, LogTail Tail)
    {
        public bool IsActive { get; set; }
    }

    record ProcessWatch(DateTimeOffset FirstSeen, Dictionary<string, long> SizesWhenFirstSeen)
    {
        /// <summary>We've either seen a log grow or shown the hint, so there's nothing more to check.</summary>
        public bool Settled { get; set; }
    }
}
