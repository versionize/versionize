using System.Text.Json;
using Versionize.CommandLine;

namespace Versionize.Config;

public sealed class FileConfig
{
    public string? Extends { get; set; }
    public bool? Silent { get; set; }
    public bool? DryRun { get; set; }
    public string? ReleaseAs { get; set; }
    public bool? SkipDirty { get; set; }
    public bool? SkipCommit { get; set; }
    public bool? SkipTag { get; set; }
    public bool? SkipChangelog { get; set; }
    public bool? TagOnly { get; set; }
    public bool? IgnoreInsignificantCommits { get; set; }
    public bool? ExitInsignificantCommits { get; set; }
    public string? CommitSuffix { get; set; }
    public string? Prerelease { get; set; }
    public bool? AggregatePrereleases { get; set; }
    /// <summary>
    /// Ignore commits beyond the first parent.
    /// </summary>
    public bool? FirstParentOnlyCommits { get; set; }
    public bool? Sign { get; set; }

    public CommitParserOptions? CommitParser { get; set; }
    public ProjectOptions[] Projects { get; set; } = [];
    public ChangelogOptions? Changelog { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static FileConfig? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var jsonString = File.ReadAllText(filePath);
            return Deserialize(jsonString, filePath);
        }
        catch (Exception e)
        {
            CommandLineUI.Exit($"Failed to parse .versionize file: {e.Message}", 1);
            return null;
        }
    }

    public static FileConfig? Deserialize(string jsonString, string sourceDescription)
    {
        try
        {
            return JsonSerializer.Deserialize<FileConfig>(jsonString, JsonOptions);
        }
        catch (Exception e)
        {
            CommandLineUI.Exit($"Failed to parse .versionize config from {sourceDescription}: {e.Message}", 1);
            return null;
        }
    }
}
