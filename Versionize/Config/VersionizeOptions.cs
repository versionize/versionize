namespace Versionize.Config;

public sealed class VersionizeOptions
{
    /// <inheritdoc cref="FileConfig.DryRun"/>
    public bool DryRun { get; set; }

    /// <inheritdoc cref="FileConfig.ReleaseAs"/>
    public string? ReleaseAs { get; set; }

    /// <inheritdoc cref="FileConfig.SkipDirty"/>
    public bool SkipDirty { get; set; }

    /// <inheritdoc cref="FileConfig.SkipCommit"/>
    public bool SkipCommit { get; set; }

    /// <inheritdoc cref="FileConfig.SkipTag"/>
    public bool SkipTag { get; set; }

    /// <inheritdoc cref="FileConfig.SkipChangelog"/>
    public bool SkipChangelog { get; set; }

    /// <inheritdoc cref="FileConfig.IgnoreInsignificantCommits"/>
    public bool IgnoreInsignificantCommits { get; set; }

    /// <inheritdoc cref="FileConfig.ExitInsignificantCommits"/>
    public bool ExitInsignificantCommits { get; set; }

    /// <inheritdoc cref="FileConfig.CommitSuffix"/>
    public string? CommitSuffix { get; set; }

    /// <inheritdoc cref="FileConfig.Prerelease"/>
    public string? Prerelease { get; set; }

    /// <inheritdoc cref="FileConfig.AggregatePrereleases"/>
    public bool AggregatePrereleases { get; set; }

    /// <inheritdoc cref="FileConfig.FirstParentOnlyCommits"/>
    public bool FirstParentOnlyCommits { get; set; }

    /// <inheritdoc cref="FileConfig.Sign"/>
    public bool Sign { get; set; }

    /// <summary>
    /// Identifies the type of file where version should be read from and written to.
    /// Determined automatically by <see cref="BumpFiles.BumpFileTypeDetector"/>.
    /// </summary>
    public BumpFileType BumpFileType { get; set; } = BumpFileType.Dotnet;

    /// <inheritdoc cref="CliConfig.WorkingDirectory"/>
    public string WorkingDirectory { get; set; } = "";

    /// <inheritdoc cref="FileConfig.CommitParser"/>
    public CommitParserOptions CommitParser { get; set; } = CommitParserOptions.Default;

    /// <summary>
    /// Settings for selecting and organizing projects in this repository, useful for monorepos.
    /// This controls whether you version a single project or multiple projects and how they are identified.
    /// </summary>
    public ProjectOptions Project { get; set; } = ProjectOptions.DefaultOneProjectPerRepo;

    /// <inheritdoc cref="CliConfig.FindReleaseCommitViaMessage"/>
    public bool FindReleaseCommitViaMessage { get; set; }

    /// <summary>
    /// Indicates whether git user configuration is required for this run.
    /// For example, if commits or tags need to be created then this returns true.
    /// </summary>
    public bool IsCommitConfigurationRequired()
    {
        return (!SkipCommit || !SkipTag) && !DryRun;
    }
}
