namespace FZScout;

/// <summary>Running from a build folder instead of an installation, so there's nothing for Velopack to update.</summary>
record NotInstalledSoCannotUpdate : UpdateCheckResult;
