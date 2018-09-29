using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

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

            var optionWorkingDirectory = app.Option("-w|--workingDir <WORKING_DIRECTORY>", "directory containing projects to version", CommandOptionType.SingleValue);
            var optionDryRun = app.Option("-d|--dry-run", "skip changing versions in projects, changelog generation and git commit", CommandOptionType.NoValue);
            var optionSkipDirty = app.Option("--skip-dirty", "skip git dirty check", CommandOptionType.NoValue);
            var optionReleaseAs = app.Option("-r|--release-as <VERSION>", "specify the release version manually", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                WorkingCopy
                    .Discover(optionWorkingDirectory.Value() ?? Directory.GetCurrentDirectory())
                    .Versionize(dryrun: optionDryRun.HasValue(), skipDirtyCheck: optionSkipDirty.HasValue(), releaseVersion: optionReleaseAs.Value());

                return 0;
            });

            return app.Execute(args);
        }
    }
}
