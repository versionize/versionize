using McMaster.Extensions.CommandLineUtils;

namespace Versionize.Config;

public sealed class CliConfig
{
    public CommandOption Silent { get; init; }
    public CommandOption DryRun { get; init; }
    public CommandOption ReleaseAs { get; init; }
    public CommandOption SkipDirty { get; init; }
    public CommandOption SkipCommit { get; init; }
    public CommandOption SkipTag { get; init; }
    public CommandOption SkipChangelog { get; set; }
    public CommandOption TagOnly { get; init; }
    public CommandOption IgnoreInsignificant { get; init; }
    public CommandOption ExitInsignificant { get; init; }
    public CommandOption CommitSuffix { get; init; }
    public CommandOption Prerelease { get; init; }
    public CommandOption AggregatePrereleases { get; init; }

    public CommandOption WorkingDirectory { get; init; }
    public CommandOption ConfigurationDirectory { get; init; }
    public CommandOption ProjectName { get; init; }
    public CommandOption UseCommitMessageInsteadOfTagToFindLastReleaseCommit { get; init; }

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
                CommandOptionType.NoValue),

            DryRun = app.Option(
                "-d|--dry-run",
                "Skip changing versions in projects, changelog generation and git commit",
                CommandOptionType.NoValue),

            ReleaseAs = app.Option(
                "-r|--release-as <VERSION>",
                "Specify the release version manually",
                CommandOptionType.SingleValue),

            SkipDirty = app.Option(
                "--skip-dirty",
                "Skip git dirty check",
                CommandOptionType.NoValue),

            SkipCommit = app.Option(
                "--skip-commit",
                "Skip commit and git tag after updating changelog and incrementing the version",
                CommandOptionType.NoValue),

            SkipTag = app.Option(
                "--skip-tag",
                "Skip git tag after making release commit",
                CommandOptionType.NoValue),

            SkipChangelog = app.Option(
                "--skip-changelog",
                "Skip changelog generation",
                CommandOptionType.NoValue),

            TagOnly = app.Option(
                "--tag-only",
                "Only works with git tags, does not commit or modify the csproj file.",
                CommandOptionType.NoValue),

            IgnoreInsignificant = app.Option(
                "-i|--ignore-insignificant-commits",
                "Do not bump the version if no significant commits (fix, feat or BREAKING) are found",
                CommandOptionType.NoValue),

            ExitInsignificant = app.Option(
                "--exit-insignificant-commits",
                "Exits with a non zero exit code if no significant commits (fix, feat or BREAKING) are found",
                CommandOptionType.NoValue),

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
                CommandOptionType.NoValue),

            ProjectName = app.Option(
                "--proj-name",
                "Name of a project defined in the configuration file (for monorepos)",
                CommandOptionType.SingleValue),

            UseCommitMessageInsteadOfTagToFindLastReleaseCommit = app.Option(
                "--find-release-commit-via-message",
                "Use commit message instead of tag to find last release commit",
                CommandOptionType.NoValue),
        };
    }
}
