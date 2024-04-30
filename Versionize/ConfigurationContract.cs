namespace Versionize;

public class ConfigurationContract
{
    public bool? Silent { get; set; }
    public bool? DryRun { get; set; }
    public bool? SkipDirty { get; set; }
    public bool? SkipCommit { get; set; }
    public String ReleaseAs { get; set; }
    public bool? IgnoreInsignificantCommits { get; set; }
    public bool? ExitInsignificantCommits { get; set; }
    public bool? ChangelogAll { get; set; }
    public String CommitSuffix { get; set; }
    public CommitParserOptions CommitParser { get; set; }
    public ProjectOptions[] Projects { get; set; } = Array.Empty<ProjectOptions>();
    public ChangelogOptions Changelog { get; set; }
    public string Prerelease { get; set; }
}
