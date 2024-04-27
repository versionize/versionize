﻿namespace Versionize;

public class VersionizeOptions
{
    public bool DryRun { get; set; }
    public bool SkipDirty { get; set; }
    public bool SkipCommit { get; set; }
    public bool TagOnly { get; set; }
    public bool SkipTag { get; set; }
    public String ReleaseAs { get; set; }
    public bool IgnoreInsignificantCommits { get; set; }
    public bool ExitInsignificantCommits { get; set; }
    public String CommitSuffix { get; set; }
    public string Prerelease { get; set; }
    public CommitParserOptions CommitParser { get; set; } = CommitParserOptions.Default;
    public ProjectOptions Project { get; set; } = ProjectOptions.DefaultOneProjectPerRepo;
    public bool AggregatePrereleases { get; set; }

    /// <summary>
    /// Instead of looking for a version tag, look for the last commit
    /// that starts with "chore(release):"
    /// </summary>
    /// <remarks>
    /// Use case: user doesn't tag pre-releases (skip-tag), so the only way to get the
    /// commit of the last release is to look for the last commit that contains a release message.
    /// </remarks>
    public bool UseCommitMessageInsteadOfTagToFindLastReleaseCommit { get; set; }
}
