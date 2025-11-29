using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Commands;
using Versionize.Git;
using Versionize.BumpFiles;
using Versionize.Lifecycle;

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
            .AddSingleton<IInspectCmdContextProvider, InspectCmdContextProvider>()
            .AddSingleton<IChangelogCmdContextProvider, ChangelogCmdContextProvider>()
            .AddSingleton<IVersionizeOptionsProvider, VersionizeOptionsProvider>()
            .AddSingleton<IRepositoryProvider, RepositoryProvider>()
            .AddSingleton<IRepoStateValidator, RepoStateValidator>()
            .AddSingleton<IBumpFileProvider, BumpFileProvider>()
            // Pipeline
            .AddSingleton<IVersionizeCmdPipeline, VersionizeCmdPipeline>()
            .AddSingleton<IConventionalCommitProvider, ConventionalCommitProvider>()
            .AddSingleton<IVersionBumper, VersionBumper>()
            .AddSingleton<IBumpFileUpdater, BumpFileUpdater>()
            .AddSingleton<IChangelogUpdater, ChangelogUpdater>()
            .AddSingleton<IReleaseCommitter, ReleaseCommitter>()
            .AddSingleton<IReleaseTagger, ReleaseTagger>()
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
