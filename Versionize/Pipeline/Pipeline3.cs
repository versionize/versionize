using Jab;
using LibGit2Sharp;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Lifecycle;
using Versionize.Pipeline.VersionizeSteps;

namespace Versionize.Pipeline;

public class GetBumpFilePipeline(
    VersionizeOptions _options,
    GetBumpFileStep _step,
    Func<GetBumpFileResult, ReadVersionPipeline> _readVersionPipelineFactory,
    InitWorkingCopyResult _input)
{
    private GetBumpFileResult? _output;

    public ReadVersionPipeline GetBumpFile()
    {
        _output = _step.Execute(_input, _options);
        return _readVersionPipelineFactory.Invoke(_output);
    }
}

public class ReadVersionPipeline(
    VersionizeOptions _options,
    ReadVersionStep _step,
    Func<ReadVersionResult, ParseCommitsSinceLastVersionPipeline> _parseCommitsSinceLastVersionPipelineFactory,
    GetBumpFileResult _input)
{
    private ReadVersionResult? _output;

    public ParseCommitsSinceLastVersionPipeline ReadVersion()
    {
        _output = _step.Execute(_input, _options);
        return _parseCommitsSinceLastVersionPipelineFactory.Invoke(_output);
    }
}

public class ParseCommitsSinceLastVersionPipeline(
    VersionizeOptions _options,
    ParseCommitsSinceLastVersionStep _step,
    ReadVersionResult _input)
{
    private ParseCommitsSinceLastVersionResult? _output;

    public ParseCommitsSinceLastVersionResult ParseCommitsSinceLastVersion()
    {
        _output = _step.Execute(_input, _options);
        return _output;
    }
}

// Returns Repository, VersionizeOptions, and BumpFile
public record InitWorkingCopyResult2(
    Repository Repository,
    VersionizeOptions Options,
    IBumpFile BumpFile);
public interface IInitWorkingCopyStep
{
    InitWorkingCopyResult2 Execute(CliConfig cliConfig);
}

public interface IGetBumpFileStep
{
    IBumpFile Execute(EmptyResult input, Options options);

    sealed class Options
    {
        public BumpFileType BumpFileType { get; init; }
        public string? VersionElement { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                BumpFileType = versionizeOptions.BumpFileType,
                VersionElement = versionizeOptions.Project.VersionElement,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}

public interface IReadVersionStep
{
    SemanticVersion? Execute(Repository input, Options options);

    sealed class Input
    {
        public required Repository Repository { get; init; }
        //public required IBumpFile BumpFile { get; init; }
    }

    sealed class Options
    {
        public bool TagOnly { get; init; }
        public required ProjectOptions Project { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                TagOnly = versionizeOptions.BumpFileType == BumpFileType.None,
                Project = versionizeOptions.Project,
            };
        }
    }
}

public interface IParseCommitsSinceLastVersionStep
{
    IReadOnlyList<ConventionalCommit> Execute(Repository input, Options options);

    sealed class Options
    {
        public required ProjectOptions Project { get; init; }
        public bool AggregatePrereleases { get; init; }
        public bool FindReleaseCommitViaMessage { get; init; }
        public bool FirstParentOnlyCommits { get; init; }
        public required CommitParserOptions CommitParser { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                Project = versionizeOptions.Project,
                AggregatePrereleases = versionizeOptions.AggregatePrereleases,
                FindReleaseCommitViaMessage = versionizeOptions.FindReleaseCommitViaMessage,
                FirstParentOnlyCommits = versionizeOptions.FirstParentOnlyCommits,
                CommitParser = versionizeOptions.CommitParser,
            };
        }
    }
}

public interface IBumpVersionStep
{
    SemanticVersion Execute(IReadOnlyList<ConventionalCommit> input, Options options);

    sealed class Options
    {
        public bool IgnoreInsignificantCommits { get; init; }
        public bool ExitInsignificantCommits { get; init; }
        public string? Prerelease { get; init; }
        public string? ReleaseAs { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                IgnoreInsignificantCommits = versionizeOptions.IgnoreInsignificantCommits,
                ExitInsignificantCommits = versionizeOptions.ExitInsignificantCommits,
                Prerelease = versionizeOptions.Prerelease,
                ReleaseAs = versionizeOptions.ReleaseAs,
            };
        }
    }
}

public interface IUpdateBumpFileStep
{
    void Execute(SemanticVersion input, Options options);

    sealed class Options
    {
        public bool DryRun { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
            };
        }
    }
}

public interface IUpdateChangelogStep
{
    Changelog.Changelog? Execute(Input input, Options options);

    sealed class Input
    {
        public required Repository Repository { get; init; }
        public required SemanticVersion BumpedVersion { get; init; }
        public required SemanticVersion? OriginalVersion { get; init; }
        public required IReadOnlyList<ConventionalCommit> ConventionalCommits { get; init; }
    }

    sealed class Options
    {
        public bool SkipChangelog { get; init; }
        public bool DryRun { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipChangelog = versionizeOptions.SkipChangelog,
                Project = versionizeOptions.Project,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}

public interface ICreateCommitStep
{
    Commit Execute(Input input, Options options);

    class Input
    {
        public required Repository Repo { get; init; }
        public required SemanticVersion NewVersion { get; init; }
        public required IBumpFile BumpFile { get; init; }
        public required Changelog.Changelog Changelog { get; init; }
    }

    sealed class Options
    {
        public bool SkipCommit { get; init; }
        public bool DryRun { get; init; }
        public bool Sign { get; init; }
        public string? CommitSuffix { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                CommitSuffix = versionizeOptions.CommitSuffix,
                DryRun = versionizeOptions.DryRun,
                Sign = versionizeOptions.Sign,
                SkipCommit = versionizeOptions.SkipCommit,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}

public interface ICreateTagStep
{
    Tag Execute(Input input, Options options);

    sealed class Input
    {
        public required Repository Repo { get; init; }
        public required SemanticVersion NewVersion { get; init; }
    }

    sealed class Options
    {
        public bool SkipTag { get; init; }
        public bool DryRun { get; init; }
        public bool Sign { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                Sign = versionizeOptions.Sign,
                SkipTag = versionizeOptions.SkipTag,
                Project = versionizeOptions.Project,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}

public sealed class WorkingCopy2
{
    public required IRepository Repository { get; init; }
    public required ProjectOptions ProjectOptions { get; init; }
}

public class Pipeline3 :
    IInitWorkingCopyPipeline,
    IGetBumpFilePipeline,
    IReadVersionPipeline,
    IParseCommitsSinceLastVersionPipeline,
    IBumpVersionPipeline,
    IUpdateBumpFilePipeline,
    IUpdateChangelogPipeline,
    ICreateCommitPipeline,
    ICreateTagPipeline
{
    // Steps
    private readonly CliConfig _cliConfig;
    private readonly IInitWorkingCopyStep _initWorkingCopyStep;
    private readonly IReadVersionStep _readVersionStep;
    private readonly IParseCommitsSinceLastVersionStep _parseCommitsSinceLastVersionStep;
    private readonly IBumpVersionStep _bumpVersionStep;
    private readonly IUpdateBumpFileStep _updateBumpFileStep;
    private readonly IUpdateChangelogStep _updateChangelogStep;
    private readonly ICreateCommitStep _createCommitStep;
    private readonly ICreateTagStep _createTagStep;

    // Results
    private InitWorkingCopyResult2? _initWorkingCopyResult;
    private SemanticVersion? _readVersionResult;
    private IReadOnlyList<ConventionalCommit>? _conventionalCommits;
    private SemanticVersion? _bumpVersionResult;
    //private BumpVersionResult? _updateBumpFileResult;
    private Changelog.Changelog? _updateChangelogResult;
    private LibGit2Sharp.Commit? _createCommitResult;
    private LibGit2Sharp.Tag? _createTagResult;

    private VersionizeOptions? _versionizeOptions;

    public Pipeline3(
        CliConfig cliConfig,
        IInitWorkingCopyStep initWorkingCopyStep,
        IReadVersionStep readVersionStep,
        IParseCommitsSinceLastVersionStep parseCommitsSinceLastVersionStep,
        IBumpVersionStep bumpVersionStep,
        IUpdateBumpFileStep updateBumpFileStep,
        IUpdateChangelogStep updateChangelogStep,
        ICreateCommitStep createCommitStep,
        ICreateTagStep createTagStep)
    {
        _cliConfig = cliConfig;
        _initWorkingCopyStep = initWorkingCopyStep;
        _readVersionStep = readVersionStep;
        _parseCommitsSinceLastVersionStep = parseCommitsSinceLastVersionStep;
        _bumpVersionStep = bumpVersionStep;
        _updateBumpFileStep = updateBumpFileStep;
        _updateChangelogStep = updateChangelogStep;
        _createCommitStep = createCommitStep;
        _createTagStep = createTagStep;
    }

    public IGetBumpFilePipeline InitWorkingCopy()
    {
        _initWorkingCopyResult = _initWorkingCopyStep.Execute(_cliConfig);
        _versionizeOptions = _initWorkingCopyResult.Options;
        return this;
    }

    public IReadVersionPipeline GetBumpFile()
    {
        return this;
    }

    public IParseCommitsSinceLastVersionPipeline ReadVersion()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_initWorkingCopyResult);
        _readVersionResult = _readVersionStep.Execute(_initWorkingCopyResult.Repository, _versionizeOptions);
        return this;
    }

    public IBumpVersionPipeline ParseCommitsSinceLastVersion()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_initWorkingCopyResult);
        _conventionalCommits = _parseCommitsSinceLastVersionStep.Execute(_initWorkingCopyResult.Repository, _versionizeOptions);
        return this;
    }

    public IUpdateBumpFilePipeline BumpVersion()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_conventionalCommits);
        _bumpVersionResult = _bumpVersionStep.Execute(_conventionalCommits, _versionizeOptions);
        return this;
    }

    public IUpdateChangelogPipeline UpdateBumpFile()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_bumpVersionResult);
        _updateBumpFileStep.Execute(_bumpVersionResult, _versionizeOptions);
        return this;
    }

    public ICreateCommitPipeline UpdateChangelog()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_initWorkingCopyResult);
        ThrowHelper.ThrowIfNull(_bumpVersionResult);
        ThrowHelper.ThrowIfNull(_readVersionResult);
        IUpdateChangelogStep.Input input = new()
        {
            Repository = _initWorkingCopyResult.Repository,
            BumpedVersion = _bumpVersionResult,
            OriginalVersion = _readVersionResult,
            ConventionalCommits = _conventionalCommits!,
        };
        _updateChangelogResult = _updateChangelogStep.Execute(input, _versionizeOptions);
        return this;
    }

    public ICreateTagPipeline CreateCommit()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_initWorkingCopyResult);
        ThrowHelper.ThrowIfNull(_bumpVersionResult);
        ThrowHelper.ThrowIfNull(_updateChangelogResult);
        ICreateCommitStep.Input input = new()
        {
            Repo = _initWorkingCopyResult.Repository,
            NewVersion = _bumpVersionResult,
            BumpFile = _initWorkingCopyResult.BumpFile,
            Changelog = _updateChangelogResult,
        };
        _createCommitResult = _createCommitStep.Execute(input, _versionizeOptions);
        return this;
    }

    public void CreateTag()
    {
        ThrowHelper.ThrowIfNull(_versionizeOptions);
        ThrowHelper.ThrowIfNull(_initWorkingCopyResult);
        ThrowHelper.ThrowIfNull(_bumpVersionResult);
        ICreateTagStep.Input input = new()
        {
            Repo = _initWorkingCopyResult.Repository,
            NewVersion = _bumpVersionResult,
        };
        _createTagResult = _createTagStep.Execute(input, _versionizeOptions);
    }
}

public static class ThrowHelper
{
    public static void ThrowIfNull(
        [NotNull] object? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}


public interface IInitWorkingCopyPipeline
{
    IGetBumpFilePipeline InitWorkingCopy();
}
public interface IGetBumpFilePipeline
{
    IReadVersionPipeline GetBumpFile();
}
public interface IReadVersionPipeline
{
    IParseCommitsSinceLastVersionPipeline ReadVersion();
}
public interface IParseCommitsSinceLastVersionPipeline
{
    IBumpVersionPipeline ParseCommitsSinceLastVersion();
}
public interface IBumpVersionPipeline
{
    IUpdateBumpFilePipeline BumpVersion();
}
public interface IUpdateBumpFilePipeline
{
    IUpdateChangelogPipeline UpdateBumpFile();
}
public interface IUpdateChangelogPipeline
{
    ICreateCommitPipeline UpdateChangelog();
}
public interface ICreateCommitPipeline
{
    ICreateTagPipeline CreateCommit();
}
public interface ICreateTagPipeline
{
    void CreateTag();
}

public interface IVersionizePipeline
{
    void Begin(VersionizeOptions options);
}

// [ServiceProvider]
// [Singleton<BumpVersionStep>]
public partial class Program2
{
    public static int Main(string[] args)
    {
        //var app = new CommandLineApplication<VersionizeCommand2>();
        var services = new ServiceCollection()
            .AddSingleton<IConsole>(PhysicalConsole.Singleton)
            .BuildServiceProvider();

        var app = new CommandLineApplication<VersionizeCommand2>();
        app.AddOption(new CommandOption("--example", CommandOptionType.SingleValue)
        {
            Description = "An example option for the main command"
        });
        app.Conventions
            .UseDefaultConventions()
            .UseConstructorInjection(services);
        return app.Execute(args);
    }
}

[Command(Name = "di", Description = "Dependency Injection sample project")]
[Subcommand(typeof(MySubcommand2))]
[HelpOption]
public class VersionizeCommand2
{
    public int OnExecute()
    {
        Console.WriteLine("VersionizeCommand executed");
        return 0;
        // var options = optionsProvider.GetOptions();
        // pipeline
        //     .InitWorkingCopy()
        //     .ReadVersion()
        //     .ParseCommitsSinceLastVersion()
        //     .BumpVersion()
        //     .UpdateBumpFile()
        //     .UpdateChangelog()
        //     .CreateCommit()
        //     .CreateTag();

        // versionizePipeline.Begin(options)
        //     .InitWorkingCopy()
        //     .GetCurrentVersion()
        //     .ParseCommitsSinceLastVersion()
        //     .BumpVersion()
        //     .UpdateBumpFile()
        //     .UpdateChangelog()
        //     .CreateCommit()
        //     .CreateTag();
    }

    // private static VersionizeOptions GetVersionizeOptions(CliConfig cliConfig)
    // {
    //     var cwd = cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
    //     var configDirectory = cliConfig.ConfigurationDirectory.Value() ?? cwd;
    //     var fileConfigPath = Path.Join(configDirectory, ".versionize");
    //     var fileConfig = FileConfig.Load(fileConfigPath);
    //     var mergedOptions = ConfigProvider.GetSelectedOptions(cwd, cliConfig, fileConfig);

    //     return mergedOptions;
    // }
}

public sealed class VersionizeCmdContextBuilder(
    IVersionizeOptionsProvider _optionsProvider,
    IRepositoryProvider _repositoryProvider,
    BumpFileProvider _bumpFileProvider)
{
    public VersionizeCmdContext Build()
    {
        var options = _optionsProvider.GetOptions();
        var repository = _repositoryProvider.GetRepository(options.WorkingDirectory);
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        // Validate repo state
        return new VersionizeCmdContext(
            options,
            bumpFile,
            repository
        );
    }
}

public sealed record VersionizeCmdContext(
    VersionizeOptions Options,
    IBumpFile BumpFile,
    Repository Repository)
{
    // public SemanticVersion? GetCurrentVersion()
    // {
    //     if (Options.TagOnly)
    //     {
    //         return Repository.Tags
    //             .Select(Options.Project.ExtractTagVersion)
    //             .Where(x => x is not null)
    //             .OrderByDescending(x => x)
    //             .FirstOrDefault();
    //     }

    //     return BumpFile.Version;
    // }
}

public sealed record InspectCmdContext(
    VersionizeOptions Options,
    IBumpFile BumpFile,
    Repository Repository);

public sealed record ChangelogCmdContext(
    VersionizeOptions Options,
    Repository Repository,
    string? VersionStr,
    string? Preamble);

public interface IVersionizeOptionsProvider
{
    VersionizeOptions GetOptions();
}
public class VersionizeOptionsProvider : IVersionizeOptionsProvider
{
    private readonly CliConfig _cliConfig;
    private readonly FileConfigProvider _fileConfigProvider;
    private readonly ConfigMerger _configMerger;
    public VersionizeOptions GetOptions()
    {
        var cwd = _cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
        var configDirectory = _cliConfig.ConfigurationDirectory.Value() ?? cwd;
        var fileConfigPath = Path.Join(configDirectory, ".versionize");
        var fileConfig = _fileConfigProvider.Load(fileConfigPath);
        var mergedOptions = _configMerger.Merge(cwd, _cliConfig, fileConfig);
        return mergedOptions;
    }
}
public class FileConfigProvider
{
    public FileConfig Load(string path)
    {
        throw new NotImplementedException();
    }
}
public class ConfigMerger
{
    public VersionizeOptions Merge(string workingDirectory, CliConfig cliConfig, FileConfig fileConfig)
    {
        throw new NotImplementedException();
    }
}

public interface IRepositoryProvider
{
    Repository GetRepository(string workingDirectoryPath);
}

public record InspectCmdOptions(
    string WorkingDirectory,
    ProjectOptions ProjectOptions,
    bool TagOnly);

public record ChangelogCmdOptions(
    bool TagOnly,
    string WorkingDirectory,
    ProjectOptions ProjectOptions,
    bool FindReleaseCommitViaMessage,
    bool FirstParentOnlyCommits,
    CommitParserOptions CommitParser,
    bool AggregatePrereleases,
    string? VersionStr,
    string? Preamble);

[Command(Name = "mysubcommand", Description = "A sample subcommand")]
public class MySubcommand2
{
    [Option(Description = "An example option")]
    public string ExampleOption { get; } = "default value";
{
    [Option(Description = "An example option")]
    public bool A { get; }

    public MySubcommand2(IEnumerable<CommandOption> options)
    {
        Console.WriteLine(string.Join(", ", options.Select(o => $"{o.LongName}={o.Value()}")));
    }

    protected void OnExecute()
    {
        Console.WriteLine("MySubcommand executed");
    }
}
