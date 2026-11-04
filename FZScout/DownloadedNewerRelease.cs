using Velopack;

namespace FZScout;

/// <summary>The release gets installed when FZScout exits.</summary>
record DownloadedNewerRelease(SemanticVersion Version) : UpdateCheckResult;
