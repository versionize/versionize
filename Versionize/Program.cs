using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
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
            var optionSilent = app.Option("--silent", "Supress output to console", CommandOptionType.NoValue);

            var optionSkipCommit = app.Option("--skip-commit", "Skip commit and git tag after updating changelog and incrementing the version", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                CommandLineUI.Verbosity = optionSilent.HasValue()?LogLevel.Silent:LogLevel.All;

                WorkingCopy
                    .Discover(optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory())
                    .Versionize(
                        dryrun: optionDryRun.HasValue(), 
                        skipDirtyCheck: optionSkipDirty.HasValue(), 
                        skipCommit: optionSkipCommit.HasValue(), 
                        releaseVersion: optionReleaseAs.Value());

                return 0;
            });

            return app.Execute(args);
        }

        static string GetVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
