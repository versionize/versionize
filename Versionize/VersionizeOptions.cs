﻿namespace Versionize;

public class VersionizeOptions
{
    public bool DryRun { get; set; }
    public bool SkipDirty { get; set; }
    public bool SkipCommit { get; set; }
    public String ReleaseAs { get; set; }
    public bool IgnoreInsignificantCommits { get; set; }
    public String CommitSuffix { get; set; }
    public string Prerelease { get; set; }
    public ChangelogOptions Changelog { get; set; } = ChangelogOptions.Default;
}
