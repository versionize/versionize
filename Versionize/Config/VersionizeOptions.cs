namespace Versionize.Config;

public sealed class VersionizeOptions
{
    public bool DryRun { get; set; }
    public string? ReleaseAs { get; set; }
    public bool SkipDirty { get; set; }
    public bool SkipCommit { get; set; }
    public bool SkipTag { get; set; }
    public bool SkipChangelog { get; set; }
    public bool IgnoreInsignificantCommits { get; set; }
    public bool ExitInsignificantCommits { get; set; }
    public string? CommitSuffix { get; set; }
    public string? Prerelease { get; set; }
    public bool AggregatePrereleases { get; set; }
    /// <summary>
    /// Ignore commits beyond the first parent.
    /// </summary>
    public bool FirstParentOnlyCommits { get; set; }
    public bool Sign { get; set; }
    public BumpFileType BumpFileType { get; set; } = BumpFileType.Dotnet;
    public string? VersionElement { get; set; }

    public string WorkingDirectory { get; set; } = "";
    public CommitParserOptions CommitParser { get; set; } = CommitParserOptions.Default;
    public ProjectOptions Project { get; set; } = ProjectOptions.DefaultOneProjectPerRepo;

    /// <summary>
    /// Instead of looking for a version tag, look for the last commit
    /// that starts with "chore(release):"
    /// </summary>
    /// <remarks>
    /// Use case: user doesn't tag pre-releases (skip-tag), so the only way to get the
    /// commit of the last release is to look for the last commit that contains a release message.
    /// </remarks>
    public bool UseCommitMessageInsteadOfTagToFindLastReleaseCommit { get; set; }

    public bool IsCommitConfigurationRequired()
    {
        return (!SkipCommit || !SkipTag) && !DryRun;
    }
}
