using McMaster.Extensions.CommandLineUtils;
using Versionize.Config.Validation;

namespace Versionize.Config;

public sealed class CliConfig
{
    /// <inheritdoc cref="FileConfig.Silent"/>
    public required CommandOption<bool> Silent { get; init; }

    /// <inheritdoc cref="FileConfig.DryRun"/>
    public required CommandOption<bool> DryRun { get; init; }

    /// <inheritdoc cref="FileConfig.ReleaseAs"/>
    public required CommandOption ReleaseAs { get; init; }

    /// <inheritdoc cref="FileConfig.SkipDirty"/>
    public required CommandOption<bool> SkipDirty { get; init; }

    /// <inheritdoc cref="FileConfig.SkipCommit"/>
    public required CommandOption<bool> SkipCommit { get; init; }

    /// <inheritdoc cref="FileConfig.SkipTag"/>
    public required CommandOption<bool> SkipTag { get; init; }

    /// <inheritdoc cref="FileConfig.SkipChangelog"/>
    public required CommandOption<bool> SkipChangelog { get; init; }

    /// <inheritdoc cref="FileConfig.TagOnly"/>
    public required CommandOption<bool> TagOnly { get; init; }

    /// <inheritdoc cref="FileConfig.IgnoreInsignificantCommits"/>
    public required CommandOption<bool> IgnoreInsignificant { get; init; }

    /// <inheritdoc cref="FileConfig.ExitInsignificantCommits"/>
    public required CommandOption<bool> ExitInsignificant { get; init; }

    /// <inheritdoc cref="FileConfig.CommitSuffix"/>
    public required CommandOption CommitSuffix { get; init; }

    /// <inheritdoc cref="FileConfig.Prerelease"/>
    public required CommandOption Prerelease { get; init; }

    /// <inheritdoc cref="FileConfig.AggregatePrereleases"/>
    public required CommandOption<bool> AggregatePrereleases { get; init; }

    /// <inheritdoc cref="FileConfig.FirstParentOnlyCommits"/>
    public required CommandOption<bool> FirstParentOnlyCommits { get; init; }

    /// <inheritdoc cref="FileConfig.Sign"/>
    public required CommandOption<bool> Sign { get; init; }

    /// <inheritdoc cref="FileConfig.TagTemplate"/>
    public required CommandOption TagTemplate { get; init; }

    /// <summary>
    /// The folder where Versionize will look for your project files and perform versioning operations.
    /// Set this if your code is not in the current folder.
    /// </summary>
    public required CommandOption WorkingDirectory { get; init; }

    /// <summary>
    /// The folder containing your .versionize configuration file.
    /// Set this if your config file is not in the working directory.
    /// </summary>
    public required CommandOption ConfigurationDirectory { get; init; }

    /// <summary>
    /// The name of the project to version, as defined in your configuration file.
    /// Useful for monorepos with multiple projects defined in <see cref="FileConfig.Projects"/>.
    /// </summary>
    public required CommandOption ProjectName { get; init; }

    /// <summary>
    /// Instead of looking for a version tag, look for the last commit
    /// that starts with "chore(release):"
    /// </summary>
    /// <remarks>
    /// Use case: user doesn't tag prereleases (skip-tag), so the only way to get the
    /// commit of the last release is to look for the last commit that contains a release message.
    /// </remarks>
    public required CommandOption<bool> FindReleaseCommitViaMessage { get; init; }

    public static CliConfig Create(CommandLineApplication app)
    {
        return new CliConfig
        {
            WorkingDirectory = app.Option(
                "-w|--workingDir <WORKING_DIRECTORY>",
                "Directory containing projects to version",
                CommandOptionType.SingleValue)
                .Accepts(v => v.ExistingDirectory()),

            ConfigurationDirectory = app.Option(
                "--configDir <CONFIG_DIRECTORY>",
                "Directory containing the versionize configuration file",
                CommandOptionType.SingleValue)
                .Accepts(v => v.ExistingDirectory()),

            Silent = app.Option<bool>(
                "--silent",
                "Suppress output to console",
                CommandOptionType.SingleOrNoValue),

            DryRun = app.Option<bool>(
                "-d|--dry-run",
                "Skip changing versions in projects, changelog generation and git commit",
                CommandOptionType.SingleOrNoValue),

            ReleaseAs = app.Option(
                "-r|--release-as <VERSION>",
                "Specify the release version manually",
                CommandOptionType.SingleValue)
                .Accepts(v => v.Use(SemanticVersionValidator.Default)),

            SkipDirty = app.Option<bool>(
                "--skip-dirty",
                "Skip git dirty check",
                CommandOptionType.SingleOrNoValue),

            SkipCommit = app.Option<bool>(
                "--skip-commit",
                "Skip commit and git tag after updating changelog and incrementing the version",
                CommandOptionType.SingleOrNoValue),

            SkipTag = app.Option<bool>(
                "--skip-tag",
                "Skip git tag after making release commit",
                CommandOptionType.SingleOrNoValue),

            SkipChangelog = app.Option<bool>(
                "--skip-changelog",
                "Skip changelog generation",
                CommandOptionType.SingleOrNoValue),

            TagOnly = app.Option<bool>(
                "--tag-only",
                "Only works with git tags, does not commit or modify the csproj file.",
                CommandOptionType.SingleOrNoValue),

            IgnoreInsignificant = app.Option<bool>(
                "-i|--ignore-insignificant-commits",
                "Do not bump the version if no significant commits (fix, feat or BREAKING) are found",
                CommandOptionType.SingleOrNoValue),

            ExitInsignificant = app.Option<bool>(
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
                CommandOptionType.SingleValue)
                .Accepts(v => v.Use(PrereleaseIdentifierValidator.Default)),

            AggregatePrereleases = app.Option<bool>(
                "-a|--aggregate-pre-releases",
                "Include all pre-release commits in the changelog since the last full version.",
                CommandOptionType.SingleOrNoValue),

            FirstParentOnlyCommits = app.Option<bool>(
                "--first-parent-only-commits",
                "Ignore commits beyond the first parent.",
                CommandOptionType.SingleOrNoValue),

            Sign = app.Option<bool>(
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

            FindReleaseCommitViaMessage = app.Option<bool>(
                "--find-release-commit-via-message",
                "Use commit message instead of tag to find last release commit",
                CommandOptionType.SingleOrNoValue),
        };
    }
}
