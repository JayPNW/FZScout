namespace FZScout;

/// <summary>The choices the user makes in the Settings window, other than the API key.</summary>
record Settings(bool LaunchesAtLogin, bool ShowsToastNotifications, bool DownloadsUpdatesAutomatically);
