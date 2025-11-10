using McMaster.Extensions.CommandLineUtils;

namespace Versionize.Config;

public sealed class CliConfig
{
    public required CommandOption Silent { get; init; }
    public required CommandOption DryRun { get; init; }
    public required CommandOption ReleaseAs { get; init; }
    public required CommandOption SkipDirty { get; init; }
    public required CommandOption SkipCommit { get; init; }
    public required CommandOption SkipTag { get; init; }
    public required CommandOption SkipChangelog { get; set; }
    public required CommandOption TagOnly { get; init; }
    public required CommandOption IgnoreInsignificant { get; init; }
    public required CommandOption ExitInsignificant { get; init; }
    public required CommandOption CommitSuffix { get; init; }
    public required CommandOption Prerelease { get; init; }
    public required CommandOption AggregatePrereleases { get; init; }
    /// <summary>
    /// Ignore commits beyond the first parent.
    /// </summary>
    public required CommandOption FirstParentOnlyCommits { get; init; }
    public required CommandOption Sign { get; init; }
    public required CommandOption TagTemplate { get; init; }

    public required CommandOption WorkingDirectory { get; init; }
    public required CommandOption ConfigurationDirectory { get; init; }
    public required CommandOption ProjectName { get; init; }
    public required CommandOption UseCommitMessageInsteadOfTagToFindLastReleaseCommit { get; init; }

    public static CliConfig Create(CommandLineApplication app)
    {
        return new CliConfig
        {
            WorkingDirectory = app.Option(
                "-w|--workingDir <WORKING_DIRECTORY>",
                "Directory containing projects to version",
                CommandOptionType.SingleValue),

            ConfigurationDirectory = app.Option(
                "--configDir <CONFIG_DIRECTORY>",
                "Directory containing the versionize configuration file",
                CommandOptionType.SingleValue),

            Silent = app.Option(
                "--silent",
                "Suppress output to console",
                CommandOptionType.SingleOrNoValue),

            DryRun = app.Option(
                "-d|--dry-run",
                "Skip changing versions in projects, changelog generation and git commit",
                CommandOptionType.SingleOrNoValue),

            ReleaseAs = app.Option(
                "-r|--release-as <VERSION>",
                "Specify the release version manually",
                CommandOptionType.SingleValue),

            SkipDirty = app.Option(
                "--skip-dirty",
                "Skip git dirty check",
                CommandOptionType.SingleOrNoValue),

            SkipCommit = app.Option(
                "--skip-commit",
                "Skip commit and git tag after updating changelog and incrementing the version",
                CommandOptionType.SingleOrNoValue),

            SkipTag = app.Option(
                "--skip-tag",
                "Skip git tag after making release commit",
                CommandOptionType.SingleOrNoValue),

            SkipChangelog = app.Option(
                "--skip-changelog",
                "Skip changelog generation",
                CommandOptionType.SingleOrNoValue),

            TagOnly = app.Option(
                "--tag-only",
                "Only works with git tags, does not commit or modify the csproj file.",
                CommandOptionType.SingleOrNoValue),

            IgnoreInsignificant = app.Option(
                "-i|--ignore-insignificant-commits",
                "Do not bump the version if no significant commits (fix, feat or BREAKING) are found",
                CommandOptionType.SingleOrNoValue),

            ExitInsignificant = app.Option(
                "--exit-insignificant-commits",
                "Exits with a non zero exit code if no significant commits (fix, feat or BREAKING) are found",
                CommandOptionType.SingleOrNoValue),

            CommitSuffix = app.Option(
                "--commit-suffix",
                "Suffix to be added to the end of the release commit message (e.g. [skip ci])",
                CommandOptionType.SingleValue),

            Prerelease = app.Option(
                "-p|--pre-release",
                "Release as pre-release version with given pre release label.",
                CommandOptionType.SingleValue),

            AggregatePrereleases = app.Option(
                "-a|--aggregate-pre-releases",
                "Include all pre-release commits in the changelog since the last full version.",
                CommandOptionType.SingleOrNoValue),

            FirstParentOnlyCommits = app.Option(
                "--first-parent-only-commits",
                "Ignore commits beyond the first parent.",
                CommandOptionType.SingleOrNoValue),

            Sign = app.Option(
                "-s|--sign",
                "Sign the git commit and tag.",
                CommandOptionType.SingleOrNoValue),

            TagTemplate = app.Option(
                "--tag-template <TAG_TEMPLATE>",
                "Template for git tags, e.g. {name}/v{version}",
                CommandOptionType.SingleValue),

            ProjectName = app.Option(
                "--proj-name",
                "Name of a project defined in the configuration file (for monorepos)",
                CommandOptionType.SingleValue),

            UseCommitMessageInsteadOfTagToFindLastReleaseCommit = app.Option(
                "--find-release-commit-via-message",
                "Use commit message instead of tag to find last release commit",
                CommandOptionType.SingleOrNoValue),
        };
    }
}
