using System.IO;
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
            var app = new CommandLineApplication();
            app.Name = "versionize";
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

            app.OnExecute(() =>
            {
                CommandLineUI.Verbosity = optionSilent.HasValue() ? LogLevel.Silent : LogLevel.All;

                WorkingCopy
                    .Discover(optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory())
                    .Versionize(
                        dryrun: optionDryRun.HasValue(),
                        skipDirtyCheck: optionSkipDirty.HasValue(),
                        skipCommit: optionSkipCommit.HasValue(),
                        releaseVersion: optionReleaseAs.Value(),
                        ignoreInsignificant: optionIgnoreInsignificant.HasValue(),
                        includeAllCommitsInChangelog: optionIncludeAllCommitsInChangelog.HasValue()
                    );

                return 0;
            });

            return app.Execute(args);
        }

        static string GetVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
