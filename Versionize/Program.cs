using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Config.Validation;
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

        var cliConfig = CliConfig.Create(app);

        app.Command("inspect", inspectCmd =>
        {
            inspectCmd.Description = "Prints the current version to stdout";
            inspectCmd.OnExecute(() =>
            {
                var (workingCopy, options) = GetWorkingCopy(cliConfig);
                workingCopy.Inspect(options);
            });
        });

        app.Command("changelog", changelogCmd =>
        {
            changelogCmd.Description = "Prints a given version's changelog to stdout";

            var versionOption = changelogCmd.Option(
                "-v|--version <VERSION>",
                "The version to include in the changelog",
                CommandOptionType.SingleValue)
                .Accepts(v => v.Use(SemanticVersionValidator.Default));

            var preambleOption = changelogCmd.Option(
                "-p|--preamble <PREAMBLE>",
                "Text to display before the list of commits",
                CommandOptionType.SingleValue);

            changelogCmd.OnExecute(() =>
            {
                var (workingCopy, options) = GetWorkingCopy(cliConfig);
                workingCopy.GenerateChangelog(options, versionOption.Value(), preambleOption.Value());
            });
        });

        int Versionize()
        {
            var (workingCopy, options) = GetWorkingCopy(cliConfig);
            workingCopy.Versionize(options);
            return 0;
        }

        app.OnExecute(Versionize);

        try
        {
            return app.Execute(args);
        }
        catch (VersionizeException ex)
        {
            CommandLineUI.Platform.WriteLine(ex.Message, ConsoleColor.Red);
            return ex.ExitCode;
        }
        catch (UnrecognizedCommandParsingException ex)
        {
            CommandLineUI.Platform.WriteLine(ex.Message, ConsoleColor.Red);
            return 1;
        }
        catch (LibGit2Sharp.NotFoundException ex)
        {
            CommandLineUI.Platform.WriteLine(ErrorMessages.LibGitNotFound(ex), ConsoleColor.Red);
            return 1;
        }
    }

    private static (WorkingCopy, VersionizeOptions) GetWorkingCopy(CliConfig cliConfig)
    {
        var cwd = cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
        var configDirectory = cliConfig.ConfigurationDirectory.Value() ?? cwd;
        var fileConfigPath = Path.Join(configDirectory, ".versionize");
        var fileConfig = FileConfig.Load(fileConfigPath);
        var mergedOptions = ConfigProvider.GetSelectedOptions(cwd, cliConfig, fileConfig);
        WorkingCopy workingCopy = WorkingCopy.Discover(cwd)!;

        return (workingCopy, mergedOptions);
    }

    private static string GetVersion() => typeof(Program).Assembly.GetName().Version?.ToString() ?? "";
}
