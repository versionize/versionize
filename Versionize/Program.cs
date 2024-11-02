using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;
using Versionize.Config;
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

        var inspectCmd = app.Command(
            "inspect",
            inspectCmd => inspectCmd.OnExecute(Inspect));
        inspectCmd.Description = "Prints the current version to stdout";

        app.OnExecute(() => Versionize());

        int Inspect()
        {
            return Versionize(true);
        }

        int Versionize(bool inspect = false)
        {
            var cwd = cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
            var configDirectory = cliConfig.ConfigurationDirectory.Value() ?? cwd;
            var fileConfigPath = Path.Join(configDirectory, ".versionize");
            var fileConfig = FileConfig.Load(fileConfigPath);
            var mergedOptions = ConfigProvider.GetSelectedOptions(cwd, cliConfig, fileConfig);

            WorkingCopy workingCopy = WorkingCopy.Discover(cwd)!;
            if (inspect)
            {
                workingCopy.Inspect(mergedOptions);
            }
            else
            {
                workingCopy.Versionize(mergedOptions);
            }

            return 0;
        }

        try
        {
            return app.Execute(args);
        }
        catch (Exception ex) when (
            ex is UnrecognizedCommandParsingException ||
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
    
    private static string GetVersion() => typeof(Program).Assembly.GetName().Version?.ToString() ?? "";
}
