namespace Versionize.Config;

public sealed record VersionizeOptions
{
    /// <inheritdoc cref="FileConfig.DryRun"/>
    public bool DryRun { get; init; }

    /// <inheritdoc cref="FileConfig.ReleaseAs"/>
    public string? ReleaseAs { get; init; }

    /// <inheritdoc cref="FileConfig.SkipDirty"/>
    public bool SkipDirty { get; init; }

    /// <inheritdoc cref="FileConfig.SkipCommit"/>
    public bool SkipCommit { get; init; }

    /// <inheritdoc cref="FileConfig.SkipTag"/>
    public bool SkipTag { get; init; }

    /// <inheritdoc cref="FileConfig.SkipChangelog"/>
    public bool SkipChangelog { get; init; }

    /// <inheritdoc cref="FileConfig.IgnoreInsignificantCommits"/>
    public bool IgnoreInsignificantCommits { get; init; }

    /// <inheritdoc cref="FileConfig.ExitInsignificantCommits"/>
    public bool ExitInsignificantCommits { get; init; }

    /// <inheritdoc cref="FileConfig.CommitSuffix"/>
    public string? CommitSuffix { get; init; }

    /// <inheritdoc cref="FileConfig.Prerelease"/>
    public string? Prerelease { get; init; }

    /// <inheritdoc cref="FileConfig.AggregatePrereleases"/>
    public bool AggregatePrereleases { get; init; }

    /// <inheritdoc cref="FileConfig.FirstParentOnlyCommits"/>
    public bool FirstParentOnlyCommits { get; init; }

    /// <inheritdoc cref="FileConfig.Sign"/>
    public bool Sign { get; init; }

    /// <summary>
    /// Identifies the type of file where version should be read from and written to.
    /// Determined automatically by <see cref="BumpFiles.BumpFileTypeDetector"/>.
    /// </summary>
    public BumpFileType BumpFileType { get; init; } = BumpFileType.Dotnet;

    /// <inheritdoc cref="CliConfig.WorkingDirectory"/>
    public string WorkingDirectory { get; init; } = "";

    /// <inheritdoc cref="FileConfig.CommitParser"/>
    public CommitParserOptions CommitParser { get; init; } = CommitParserOptions.Default;

    /// <summary>
    /// Settings for selecting and organizing projects in this repository, useful for monorepos.
    /// This controls whether you version a single project or multiple projects and how they are identified.
    /// </summary>
    public ProjectOptions Project { get; init; } = ProjectOptions.DefaultOneProjectPerRepo;

    /// <summary>
    /// Optional custom versioning strategy mapping commit types to positions.
    /// Comes from the top-level "versioning" section of the config file.
    /// </summary>
    public VersioningOptions? Versioning { get; init; }

    /// <summary>
    /// Commit type aliases mapping a canonical type to alternate type strings.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Aliases { get; init; }

    /// <inheritdoc cref="CliConfig.FindReleaseCommitViaMessage"/>
    public bool FindReleaseCommitViaMessage { get; init; }

    /// <summary>
    /// Indicates whether git user configuration is required for this run.
    /// For example, if commits or tags need to be created then this returns true.
    /// </summary>
    public bool IsCommitConfigurationRequired()
    {
        return (!SkipCommit || !SkipTag) && !DryRun;
    }
}
