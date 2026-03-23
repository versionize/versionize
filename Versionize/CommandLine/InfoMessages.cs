namespace Versionize.CommandLine;

/// <summary>
/// Centralized informational / status message templates to unify wording.
/// </summary>
public static class InfoMessages
{
    public static string DiscoveredVersionableProjects(int count) => $"Discovered {count} versionable projects";
    public static string ProjectFile(string path) => $"  * {path}";
    public static string UpdatedChangelog(string? file = "CHANGELOG.md") => $"updated {file}";
    public static string CommittedChanges(string changelogFile) => $"committed changes in projects and {changelogFile}";
    public static string BumpingVersion(string from, string to) => $"bumping version from {from} to {to} in projects";
    public static string TaggedRelease(string tagName, string sha) => $"tagged release as {tagName} against commit with sha {sha}";
    public static string IgnoredToolSymlinks(int count, string toolDirectoryList) =>
        $"Warning: Detected {count} symlink{(count == 1 ? "" : "s")} in tool directories ({toolDirectoryList}). Please verify them manually.";
    public static string IgnoredToolDirectoryEntries(int count, string toolDirectoryList) =>
        $"Warning: Detected {count} tool-managed entr{(count == 1 ? "y" : "ies")} in tool directories ({toolDirectoryList}) that were reported as deleted but still exist on disk. Please verify them manually.";
}
