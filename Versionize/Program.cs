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

            var optionWorkingDirectory = app.Option("-w|--workingDir <WORKING_DIRECTORY>", "directory containing projects to version", CommandOptionType.SingleValue);
            var optionDryRun = app.Option("-d|--dry-run", "skip changing versions in projects, changelog generation and git commit", CommandOptionType.NoValue);
            var optionSkipDirty = app.Option("--skip-dirty", "skip git dirty check", CommandOptionType.NoValue);
            var optionReleaseAs = app.Option("-r|--release-as <VERSION>", "specify the release version manually", CommandOptionType.SingleValue);
            var optionSilent = app.Option("--silent", "do not log to console", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                CommandLineUI.Verbosity = optionSilent.HasValue()?LogLevel.Silent:LogLevel.All;

                WorkingCopy
                    .Discover(optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory())
                    .Versionize(dryrun: optionDryRun.HasValue(), skipDirtyCheck: optionSkipDirty.HasValue(), releaseVersion: optionReleaseAs.Value());

                return 0;
            });

            return app.Execute(args);
        }

        static string GetVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
