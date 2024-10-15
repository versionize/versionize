﻿namespace Versionize.Config;

public sealed class FileConfig
{
    public bool? Silent { get; set; }
    public bool? DryRun { get; set; }
    public string ReleaseAs { get; set; }
    public bool? SkipDirty { get; set; }
    public bool? SkipCommit { get; set; }
    public bool? SkipTag { get; set; }
    public bool? SkipChangelog { get; set; }
    public bool? TagOnly { get; set; }
    public bool? IgnoreInsignificantCommits { get; set; }
    public bool? ExitInsignificantCommits { get; set; }
    /// <summary>
    /// The first parent only options allows you to ignore commits that are not the first parent.
    /// </summary>
    public bool? FirstParentOnlyCommit { get; set; }
    public string CommitSuffix { get; set; }
    public string Prerelease { get; set; }
    public bool? AggregatePrereleases { get; set; }

    public CommitParserOptions CommitParser { get; set; }
    public ProjectOptions[] Projects { get; set; } = [];
    public ChangelogOptions Changelog { get; set; }
}
