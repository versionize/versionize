using System;
using System.IO;
using System.Text.Json;
using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;

namespace Versionize
{
    [Command(
        Name = "Versionize",
        Description = "Automatic versioning and CHANGELOG generation, using conventional commit messages")]
    public class Program
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
                    ChangelogAll = optionIncludeAllCommitsInChangelog.HasValue(),
                    CommitSuffix = optionCommitSuffix.Value(),
                });

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
            catch (UnrecognizedCommandParsingException e)
            {
                return CommandLineUI.Exit(e.Message, 1);
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

        private static VersionizeOptions MergeWithOptions(ConfigurationContract optionalConfiguration, VersionizeOptions configuration)
        {
            return new VersionizeOptions
            {
                DryRun = MergeBool(configuration.DryRun, optionalConfiguration?.DryRun),
                SkipDirty = MergeBool(configuration.SkipDirty, optionalConfiguration?.SkipDirty),
                SkipCommit = MergeBool(configuration.SkipCommit, optionalConfiguration?.SkipCommit),
                ReleaseAs = configuration.ReleaseAs ?? optionalConfiguration?.ReleaseAs,
                IgnoreInsignificantCommits = MergeBool(configuration.IgnoreInsignificantCommits, optionalConfiguration?.IgnoreInsignificantCommits),
                ChangelogAll = MergeBool(configuration.ChangelogAll, optionalConfiguration?.ChangelogAll),
                CommitSuffix = configuration.CommitSuffix ?? optionalConfiguration?.CommitSuffix,
            };
        }

        private static bool MergeBool(bool overridingValue, bool? optionalValue)
        {
            return !overridingValue ? optionalValue ?? overridingValue : overridingValue;
        }
    }
}
