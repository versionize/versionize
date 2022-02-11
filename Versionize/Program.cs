using System.Text.Json;
using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;
using Versionize.Versioning;

namespace Versionize;

[Command(
    Name = "Versionize",
    Description = "Automatic versioning and CHANGELOG generation, using conventional commit messages")]
public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandLineApplication
        {
            Name = "versionize",
            UsePagerForHelpText = false
        };

        app.HelpOption();
        app.VersionOption("-v|--version", GetVersion());

        var optionWorkingDirectory = app.Option("-w|--workingDir <WORKING_DIRECTORY>", "Directory containing projects to version", CommandOptionType.SingleValue);
        var optionDryRun = app.Option("-d|--dry-run", "Skip changing versions in projects, changelog generation and git commit", CommandOptionType.NoValue);
        var optionSkipDirty = app.Option("--skip-dirty", "Skip git dirty check", CommandOptionType.NoValue);
        var optionReleaseAs = app.Option("-r|--release-as <VERSION>", "Specify the release version manually", CommandOptionType.SingleValue);
        var optionSilent = app.Option("--silent", "Suppress output to console", CommandOptionType.NoValue);

        var optionSkipCommit = app.Option("--skip-commit", "Skip commit and git tag after updating changelog and incrementing the version", CommandOptionType.NoValue);
        var optionIgnoreInsignificant = app.Option("-i|--ignore-insignificant-commits", "Do not bump the version if no significant commits (fix, feat or BREAKING) are found", CommandOptionType.NoValue);
        var optionIncludeAllCommitsInChangelog = app.Option("--changelog-all", "Include all commits in the changelog not just fix, feat and breaking changes", CommandOptionType.NoValue);
        var optionCommitSuffix = app.Option("--commit-suffix", "Suffix to be added to the end of the release commit message (e.g. [skip ci])", CommandOptionType.SingleValue);
        var optionPrerelease = app.Option("-p|--pre-release", "Release as pre-release version with given pre release label.", CommandOptionType.SingleValue);

        var inspectCmd = app.Command("inspect", inspectCmd => inspectCmd.OnExecute(() =>
        {
            var cwd = optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory();

            WorkingCopy
                .Discover(cwd)
                .Inspect();

            return 0;
        }));
        inspectCmd.Description = "Prints the current version to stdout";

        app.OnExecute(() =>
        {
            var cwd = optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
            var jsonFileConfig = FromJsonFile(Path.Join(cwd, ".versionize"));

            var options = MergeWithOptions(jsonFileConfig, new VersionizeOptions
            {
                DryRun = optionDryRun.HasValue(),
                SkipDirty = optionSkipDirty.HasValue(),
                SkipCommit = optionSkipCommit.HasValue(),
                ReleaseAs = optionReleaseAs.Value(),
                IgnoreInsignificantCommits = optionIgnoreInsignificant.HasValue(),
                CommitSuffix = optionCommitSuffix.Value(),
                Prerelease = optionPrerelease.Value(),
                Changelog = ChangelogOptions.Default,
            },
            optionIncludeAllCommitsInChangelog.HasValue());

            CommandLineUI.Verbosity = MergeBool(optionSilent.HasValue(), jsonFileConfig?.Silent) ? LogLevel.Silent : LogLevel.All;

            WorkingCopy
                .Discover(cwd)
                .Versionize(options);

            return 0;
        });

        try
        {
            return app.Execute(args);
        }
        catch (Exception ex) when (ex is UnrecognizedCommandParsingException ||
                                   ex is InvalidPrereleaseIdentifierException)
        {
            return CommandLineUI.Exit(ex.Message, 1);
        }
        catch (LibGit2Sharp.NotFoundException e)
        {
            return CommandLineUI.Exit($@"
Error: LibGit2Sharp.NotFoundException

This is most likely caused by running versionize against a git repository cloned with depth --1.
In case you're using the actions/checkout@v2 in github actions you could specify fetch-depth: '1'.
For more detail see  https://github.com/actions/checkout

Exception detail:

{e}", 1);
        }
    }

    private static string GetVersion() => typeof(Program).Assembly.GetName().Version.ToString();

    private static ConfigurationContract FromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            CommandLineUI.Information($"Reading configuration from {filePath}");

            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ConfigurationContract>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception e)
        {
            CommandLineUI.Exit($"Failed to parse .versionize file: {e.Message}", 1);
            return null;
        }
    }

    private static VersionizeOptions MergeWithOptions(
        ConfigurationContract optionalConfiguration,
        VersionizeOptions configuration,
        bool changelogAll)
    {
        var includeAllCommits = MergeBool(changelogAll, optionalConfiguration?.ChangelogAll);
        var changelog = MergeChangelogOptions(optionalConfiguration?.Changelog, configuration.Changelog);
        changelog.IncludeAllCommits = includeAllCommits;
        return new VersionizeOptions
        {
            DryRun = MergeBool(configuration.DryRun, optionalConfiguration?.DryRun),
            SkipDirty = MergeBool(configuration.SkipDirty, optionalConfiguration?.SkipDirty),
            SkipCommit = MergeBool(configuration.SkipCommit, optionalConfiguration?.SkipCommit),
            ReleaseAs = configuration.ReleaseAs ?? optionalConfiguration?.ReleaseAs,
            IgnoreInsignificantCommits = MergeBool(configuration.IgnoreInsignificantCommits, optionalConfiguration?.IgnoreInsignificantCommits),
            CommitSuffix = configuration.CommitSuffix ?? optionalConfiguration?.CommitSuffix,
            Prerelease = configuration.Prerelease ?? optionalConfiguration?.Prerelease,
            Changelog = changelog,
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
        };
    }

    private static bool MergeBool(bool overridingValue, bool? optionalValue)
    {
        return !overridingValue ? optionalValue ?? overridingValue : overridingValue;
    }
}
