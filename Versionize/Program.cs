using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Lifecycle;
using Versionize.Pipeline;
using Versionize.Pipeline.VersionizeSteps;
using Versionize.Versioning;

namespace Versionize;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandLineApplication<VersionizeCommand>();

        app.HelpOption();
        app.VersionOption("-v|--version", GetVersion());

        var cliConfig = CliConfig.Create(app);

        var services = new ServiceCollection()
            .AddSingleton(cliConfig)
            .AddSingleton<IConsole>(PhysicalConsole.Singleton)
            .AddSingleton<IVersionizeCmdContextProvider, VersionizeCmdContextProvider>()
            .AddSingleton<IVersionizeOptionsProvider, VersionizeOptionsProvider>()
            .AddSingleton<IRepositoryProvider, RepositoryProvider>()
            .AddSingleton<IBumpFileProvider, BumpFileProvider>()
            //
            .AddSingleton<IBumpVersionStep, BumpVersionStep>()
            .AddSingleton<VersionIncrementStrategy>()
            .BuildServiceProvider();

        app.Conventions
            .UseDefaultConventions()
            .UseConstructorInjection(services);

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

    private static string GetVersion() => typeof(Program).Assembly.GetName().Version?.ToString() ?? "";
}
