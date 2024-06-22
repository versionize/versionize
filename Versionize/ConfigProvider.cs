using System.Text.Json;
using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;

namespace Versionize;

public sealed class ConfigProvider
{
    private static CommandOption _optionWorkingDirectory;
    private static CommandOption _optionConfigurationDirectory;
    private static CommandOption _optionDryRun;
    private static CommandOption _optionSkipDirty;
    private static CommandOption _optionReleaseAs;
    private static CommandOption _optionSilent;
    private static CommandOption _optionSkipCommit;
    private static CommandOption _optionSkipTag;
    private static CommandOption _optionIgnoreInsignificant;
    private static CommandOption _optionExitInsignificant;
    private static CommandOption _optionCommitSuffix;
    private static CommandOption _optionPrerelease;
    private static CommandOption _optionAggregatePrereleases;
    private static CommandOption _optionUseCommitMessageInsteadOfTagToFindLastReleaseCommit;
    private static CommandOption _optionTagOnly;
    private static CommandOption _optionsProjectName;


    public static void ConfigureOptions(CommandLineApplication app)
    {
        _optionWorkingDirectory = app.Option("-w|--workingDir <WORKING_DIRECTORY>", "Directory containing projects to version", CommandOptionType.SingleValue);
        _optionConfigurationDirectory = app.Option("--configDir <CONFIG_DIRECTORY>", "Directory containing the versionize configuration file", CommandOptionType.SingleValue);
        _optionDryRun = app.Option("-d|--dry-run", "Skip changing versions in projects, changelog generation and git commit", CommandOptionType.NoValue);
        _optionSkipDirty = app.Option("--skip-dirty", "Skip git dirty check", CommandOptionType.NoValue);
        _optionReleaseAs = app.Option("-r|--release-as <VERSION>", "Specify the release version manually", CommandOptionType.SingleValue);
        _optionSilent = app.Option("--silent", "Suppress output to console", CommandOptionType.NoValue);
        _optionSkipCommit = app.Option("--skip-commit", "Skip commit and git tag after updating changelog and incrementing the version", CommandOptionType.NoValue);
        _optionSkipTag = app.Option("--skip-tag", "Skip git tag after making release commit", CommandOptionType.NoValue);
        _optionIgnoreInsignificant = app.Option("-i|--ignore-insignificant-commits", "Do not bump the version if no significant commits (fix, feat or BREAKING) are found", CommandOptionType.NoValue);
        _optionExitInsignificant = app.Option("--exit-insignificant-commits", "Exits with a non zero exit code if no significant commits (fix, feat or BREAKING) are found", CommandOptionType.NoValue);
        _optionCommitSuffix = app.Option("--commit-suffix", "Suffix to be added to the end of the release commit message (e.g. [skip ci])", CommandOptionType.SingleValue);
        _optionPrerelease = app.Option("-p|--pre-release", "Release as pre-release version with given pre release label.", CommandOptionType.SingleValue);
        _optionAggregatePrereleases = app.Option("-a|--aggregate-pre-releases", "Include all pre-release commits in the changelog since the last full version.", CommandOptionType.NoValue);
        _optionUseCommitMessageInsteadOfTagToFindLastReleaseCommit = app.Option("--find-release-commit-via-message", "Use commit message instead of tag to find last release commit", CommandOptionType.NoValue);
        _optionTagOnly = app.Option("--tag-only", "Only works with git tags, does not commit or modify the csproj file.", CommandOptionType.NoValue);
        _optionsProjectName = app.Option("--proj-name", "Name of a project defined in the configuration file (for monorepos)", CommandOptionType.SingleValue);
    }

    public static VersionizeOptions GetSelectedOptions()
    {
        var cwd = _optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
        var configDirectory = _optionConfigurationDirectory.Value() ?? cwd;
        var jsonConfigPath = Path.Join(configDirectory, ".versionize");
        var jsonFileConfig = FromJsonFile(jsonConfigPath);

        var specifiedOptions = new VersionizeOptions
        {
            WorkingDirectory = cwd,
            DryRun = _optionDryRun.HasValue(),
            SkipDirty = _optionSkipDirty.HasValue(),
            SkipChangelog = false,
            SkipCommit = _optionSkipCommit.HasValue(),
            SkipTag = _optionSkipTag.HasValue(),
            TagOnly = _optionTagOnly.HasValue(),
            ReleaseAs = _optionReleaseAs.Value(),
            IgnoreInsignificantCommits = _optionIgnoreInsignificant.HasValue(),
            ExitInsignificantCommits = _optionExitInsignificant.HasValue(),
            CommitSuffix = _optionCommitSuffix.Value(),
            Prerelease = _optionPrerelease.Value(),
            CommitParser = CommitParserOptions.Default,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            AggregatePrereleases = _optionAggregatePrereleases.HasValue(),
            UseCommitMessageInsteadOfTagToFindLastReleaseCommit =
                _optionUseCommitMessageInsteadOfTagToFindLastReleaseCommit.HasValue(),
        };

        var options = MergeWithOptions(
            jsonFileConfig,
            specifiedOptions,
            _optionsProjectName.Value());

        CommandLineUI.Verbosity = MergeBool(_optionSilent.HasValue(), jsonFileConfig?.Silent)
            ? LogLevel.Silent
            : LogLevel.All;

        return options;
    }

    private static ConfigFile FromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ConfigFile>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception e)
        {
            CommandLineUI.Exit($"Failed to parse .versionize file: {e.Message}", 1);
            return null;
        }
    }

    private static VersionizeOptions MergeWithOptions(
        ConfigFile optionalConfiguration,
        VersionizeOptions configuration,
        string projectName)
    {
        var project =
            optionalConfiguration?.Projects.FirstOrDefault(x =>
                x.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        if (project != null)
        {
            project.Changelog =
                MergeChangelogOptions(project.Changelog,
                    MergeChangelogOptions(optionalConfiguration?.Changelog, ChangelogOptions.Default));
        }
        else
        {
            project = configuration.Project;
            project.Changelog =
                MergeChangelogOptions(optionalConfiguration?.Changelog, ChangelogOptions.Default);
        }
        
        var commit = MergeCommitOptions(optionalConfiguration?.CommitParser, configuration.CommitParser);

        return new VersionizeOptions
        {
            WorkingDirectory = configuration.WorkingDirectory,
            DryRun = MergeBool(configuration.DryRun, optionalConfiguration?.DryRun),
            SkipDirty = MergeBool(configuration.SkipDirty, optionalConfiguration?.SkipDirty),
            SkipCommit = MergeBool(configuration.SkipCommit, optionalConfiguration?.SkipCommit),
            // TODO: Consider supporting optionalConfiguration
            SkipTag = configuration.SkipTag,
            TagOnly = configuration.TagOnly,
            ReleaseAs = configuration.ReleaseAs ?? optionalConfiguration?.ReleaseAs,
            IgnoreInsignificantCommits = MergeBool(configuration.IgnoreInsignificantCommits, optionalConfiguration?.IgnoreInsignificantCommits),
            ExitInsignificantCommits = MergeBool(configuration.ExitInsignificantCommits, optionalConfiguration?.ExitInsignificantCommits),
            CommitSuffix = configuration.CommitSuffix ?? optionalConfiguration?.CommitSuffix,
            Prerelease = configuration.Prerelease ?? optionalConfiguration?.Prerelease,
            CommitParser = commit,
            Project = project,
            AggregatePrereleases = configuration.AggregatePrereleases,
            // TODO: Consider supporting optionalConfiguration
            UseCommitMessageInsteadOfTagToFindLastReleaseCommit = configuration.UseCommitMessageInsteadOfTagToFindLastReleaseCommit,
        };
    }

    private static CommitParserOptions MergeCommitOptions(CommitParserOptions customOptions, CommitParserOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new CommitParserOptions
        {
            HeaderPatterns = customOptions.HeaderPatterns ?? defaultOptions.HeaderPatterns
        };
    }

    private static ChangelogOptions MergeChangelogOptions(ChangelogOptions customOptions, ChangelogOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new ChangelogOptions
        {
            Header = customOptions.Header ?? defaultOptions.Header,
            Sections = customOptions.Sections ?? defaultOptions.Sections,
            LinkTemplates = customOptions.LinkTemplates ?? defaultOptions.LinkTemplates
        };
    }

    private static bool MergeBool(bool overridingValue, bool? optionalValue)
    {
        return !overridingValue ? optionalValue ?? overridingValue : overridingValue;
    }
}
