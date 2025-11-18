using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Versionize.Config;
using Versionize.Pipeline.VersionizeSteps;

namespace Versionize.Pipeline;

public class Pipeline2<TResult>
{
    private readonly TResult _result;
    private readonly VersionizeOptions _versionizeOptions;

    public Pipeline2(TResult result, VersionizeOptions versionizeOptions)
    {
        _result = result;
        _versionizeOptions = versionizeOptions;
    }

    public Pipeline2<TResultOut> Then<TOptions, TResultOut>(
        IPipelineStep<TResult, TOptions, TResultOut> step)
        where TOptions : IConvertibleFromVersionizeOptions<TOptions>
    {
        TOptions options = TOptions.FromVersionizeOptions(_versionizeOptions);
        TResultOut result = step.Execute(_result, options);
        //TResultOut result = step.DoSomething(_result, _versionizeOptions);
        return new Pipeline2<TResultOut>(result, _versionizeOptions);
    }

    public TResult Result => _result;
}

public class Pipeline<TCurrent>
{
    private readonly IServiceProvider _services;
    private readonly TCurrent _current;
    private VersionizeOptions _versionizeOptions;

    public Pipeline(IServiceProvider services, TCurrent current, VersionizeOptions versionizeOptions)
    {
        _services = services;
        _current = current;
        _versionizeOptions = versionizeOptions;
    }

    public Pipeline<TNext> Then<TStep, TOptions, TNext>()
        where TStep : IPipelineStep<TCurrent, TOptions, TNext>
        where TOptions : IConvertibleFromVersionizeOptions<TOptions>
    {
        var step = _services.GetRequiredService<TStep>();
        var options = TOptions.FromVersionizeOptions(_versionizeOptions);
        var result = step.Execute(_current, options);
        return new Pipeline<TNext>(_services, result, _versionizeOptions);
    }

    public TCurrent Result => _current;
}

public interface IPipelineStepResolver
{
    TStep Resolve<TStep>();
}

public class Orchestrator
{
    public static int Main(string[] args)
    {
        var app = new CommandLineApplication<VersionizeCommand>();
        var cliConfig = CliConfig.Create(app);

        var services = new ServiceCollection()
            // --- Core ---
            .AddSingleton(cliConfig)
            .AddSingleton(static sp => GetVersionizeOptions(sp.GetRequiredService<CliConfig>()))
            .AddSingleton<IConsole>(PhysicalConsole.Singleton)
            // --- Versionize Steps ---
            .AddSingleton<InitWorkingCopyStep>()
            .AddSingleton<GetBumpFileStep>()
            .AddSingleton<ReadVersionStep>()
            .AddSingleton<ParseCommitsSinceLastVersionStep>()
            .AddSingleton<BumpVersionStep>()
            .AddSingleton<UpdateBumpFileStep>()
            .AddSingleton<UpdateChangelogStep>()
            .AddSingleton<CreateCommitStep>()
            .AddSingleton<CreateTagStep>()
            // ---
            .BuildServiceProvider();

        app.Conventions
            .UseDefaultConventions()
            .UseConstructorInjection(services);

        return app.Execute(args);
    }

    // TODO: Consider moving to a pipeline step for better testability.
    private static VersionizeOptions GetVersionizeOptions(CliConfig cliConfig)
    {
        var cwd = cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
        var configDirectory = cliConfig.ConfigurationDirectory.Value() ?? cwd;
        var fileConfigPath = Path.Join(configDirectory, ".versionize");
        var fileConfig = FileConfig.Load(fileConfigPath);
        var mergedOptions = ConfigProvider.GetSelectedOptions(cwd, cliConfig, fileConfig);

        return mergedOptions;
    }
}

[Command(Name = "di", Description = "Dependency Injection sample project")]
[Subcommand(typeof(MySubcommand))]
[HelpOption]
public class VersionizeCommand(
    VersionizeOptions Options,
    InitWorkingCopyStep InitWorkingCopy,
    GetBumpFileStep GetBumpFile,
    ReadVersionStep ReadVersion,
    ParseCommitsSinceLastVersionStep ParseCommitsSinceLastVersion,
    BumpVersionStep BumpVersion,
    UpdateBumpFileStep UpdateBumpFile,
    UpdateChangelogStep UpdateChangelog,
    CreateCommitStep CreateCommit,
    CreateTagStep CreateTag)
{
    public void OnExecute()
    {
        // Console.WriteLine("Starting Versionize Pipeline...");
        // return;
        Pipeline.Begin(Options)
            .Then(InitWorkingCopy)
            .Then(GetBumpFile)
            .Then(ReadVersion)
            .Then(ParseCommitsSinceLastVersion)
            .Then(BumpVersion)
            .Then(UpdateBumpFile)
            .Then(UpdateChangelog)
            .Then(CreateCommit)
            .Then(CreateTag);
    }
}

[Command(Name = "mysubcommand", Description = "A sample subcommand")]
public class MySubcommand
{
    protected void OnExecute()
    {
        Console.WriteLine("MySubcommand executed");
    }
}

public static class Lifecycle
{
    public static Pipeline<InitWorkingCopyResult> Init(VersionizeOptions options)
    {
        var pipeline = default(Pipeline<InitWorkingCopyResult>)
            .Then<GetBumpFileStep, GetBumpFileStep.Options, GetBumpFileResult>()
            .Then<ReadVersionStep, ReadVersionStep.Options, ReadVersionResult>()
            .Then<ParseCommitsSinceLastVersionStep, ParseCommitsSinceLastVersionStep.Options, ParseCommitsSinceLastVersionResult>()
            .Then<BumpVersionStep, BumpVersionStep.Options, BumpVersionResult>()
            .Then<UpdateChangelogStep, UpdateChangelogStep.Options, UpdateChangelogResult>()
            .Then<CreateCommitStep, CreateCommitStep.Options, CreateCommitResult>()
            .Then<CreateTagStep, CreateTagStep.Options, CreateTagResult>();
        // var pipeline2 = default(Pipeline<InitData>)
        //     .Then<ReadVersionStep>()
        //     .Then<GetCommitsStep>()
        //     .Then<BumpVersionStep>()
        //     .Then<UpdateChangelogStep>()
        //     .Then<CreateCommitStep>();
        var pipeline3 = default(Pipeline<InitWorkingCopyResult>)
            .GetBumpFile()
            .ReadVersion()
            .ParseCommits()
            .BumpVersion()
            .UpdateChangelog()
            .CreateCommit()
            .CreateTag();
        return new Pipeline<InitWorkingCopyResult>(
            default, //services,
            new InitWorkingCopyResult { Repository = null! }, // TODO: fix
            default
        );
    }
}
