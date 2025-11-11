namespace Versionize.CommandLine;

/// <summary>
/// Centralized informational / status message templates to unify wording.
/// </summary>
public static class InfoMessages
{
    public static string DiscoveredVersionableProjects(int count) => $"Discovered {count} versionable projects";
    public static string ProjectFile(string path) => $"  * {path}";
    public static string UpdatedChangelog(string? file = "CHANGELOG.md") => $"updated {file}";
    public static string CommittedChanges(string changelogFile = "CHANGELOG.md") => $"committed changes in projects and {changelogFile}";
    public static string BumpingVersion(string from, string to) => $"bumping version from {from} to {to} in projects";
    public static string TaggedRelease(string tagName, string sha) => $"tagged release as {tagName} against commit with sha {sha}";
}
