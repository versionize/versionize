using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.Pipeline.VersionizeSteps;

namespace Versionize.Pipeline;

public class GetBumpFilePipeline
{
    private readonly VersionizeOptions _versionizeOptions;

    // Step
    private readonly GetBumpFileStep _getBumpFileStep;

    // Temp
    private InitWorkingCopyResult _initWorkingCopyResult;
    private GetBumpFileResult _getBumpFileResult;

    public ReadVersionPipeline GetBumpFile()
    {
        _getBumpFileResult = _getBumpFileStep.Execute(_initWorkingCopyResult, _versionizeOptions);
        return new ReadVersionPipeline();
    }
}

public class ReadVersionPipeline
{
    private readonly VersionizeOptions _versionizeOptions;

    // Step
    private readonly ReadVersionStep _readVersionStep;

    // Temp
    private GetBumpFileResult _getBumpFileResult;
    private ReadVersionResult _readVersionResult;

    public ParseCommitsSinceLastVersionPipeline ReadVersion()
    {
        _readVersionResult = _readVersionStep.Execute(_getBumpFileResult, _versionizeOptions);
        return new ParseCommitsSinceLastVersionPipeline(_versionizeOptions, _readVersionResult);
    }
}

public class ParseCommitsSinceLastVersionPipeline
{
    private VersionizeOptions _versionizeOptions;
    private readonly ParseCommitsSinceLastVersionStep _parseCommitsSinceLastVersionStep;
    private ReadVersionResult _readVersionResult;
    private ParseCommitsSinceLastVersionResult _parseCommitsSinceLastVersionResult;

    public ParseCommitsSinceLastVersionPipeline(VersionizeOptions versionizeOptions, ReadVersionResult readVersionResult)
    {
        _versionizeOptions = versionizeOptions;
        _readVersionResult = readVersionResult;
    }

    public ParseCommitsSinceLastVersionResult ParseCommitsSinceLastVersion()
    {
        _parseCommitsSinceLastVersionResult = _parseCommitsSinceLastVersionStep.Execute(_readVersionResult, _versionizeOptions);
        return _parseCommitsSinceLastVersionResult;
    }
}

public class Pipeline3 :
    IGetBumpFilePipeline,
    IReadVersionPipeline,
    IParseCommitsSinceLastVersionPipeline
{
    private readonly VersionizeOptions _versionizeOptions;
    private bool _isFirstRelease;
    private IReadOnlyList<ConventionalCommit> _commits = [];
    private SemanticVersion _version;

    // Temp
    private InitWorkingCopyResult _initWorkingCopyResult;
    private GetBumpFileResult _getBumpFileResult;
    private ReadVersionResult _readVersionResult;
    private ParseCommitsSinceLastVersionResult _parseCommitsSinceLastVersionResult;

    // Steps
    private readonly InitWorkingCopyStep _initWorkingCopyStep;
    private readonly GetBumpFileStep _getBumpFileStep;
    private readonly ReadVersionStep _readVersionStep;
    private readonly ParseCommitsSinceLastVersionStep _parseCommitsSinceLastVersionStep;
    // ...

    public IReadVersionPipeline GetBumpFile()
    {
        _getBumpFileResult = _getBumpFileStep.Execute(_initWorkingCopyResult, _versionizeOptions);
        return this;
    }

    public IParseCommitsSinceLastVersionPipeline ReadVersion()
    {
        _readVersionResult = _readVersionStep.Execute(_getBumpFileResult, _versionizeOptions);
        _version = _readVersionResult.Version;
        return this;
    }

    public ParseCommitsSinceLastVersionResult ParseCommitsSinceLastVersion()
    {
        _parseCommitsSinceLastVersionResult = _parseCommitsSinceLastVersionStep.Execute(_readVersionResult, _versionizeOptions);
        _isFirstRelease = _parseCommitsSinceLastVersionResult.IsFirstRelease;
        _commits = _parseCommitsSinceLastVersionResult.Commits;
        return _parseCommitsSinceLastVersionResult;
    }
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
    ParseCommitsSinceLastVersionResult ParseCommitsSinceLastVersion();
}

[Command(Name = "di", Description = "Dependency Injection sample project")]
[Subcommand(typeof(MySubcommand2))]
[HelpOption]
public class VersionizeCommand2(
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
        IGetBumpFilePipeline p = new Pipeline3();
        p.GetBumpFile()
            .ReadVersion()
            .ParseCommitsSinceLastVersion();
        // Pipeline.Begin(Options)
        //     .Then(InitWorkingCopy)
        //     .Then(GetBumpFile)
        //     .Then(ReadVersion)
        //     .Then(ParseCommitsSinceLastVersion)
        //     .Then(BumpVersion)
        //     .Then(UpdateBumpFile)
        //     .Then(UpdateChangelog)
        //     .Then(CreateCommit)
        //     .Then(CreateTag);
    }
}

[Command(Name = "mysubcommand", Description = "A sample subcommand")]
public class MySubcommand2
{
    protected void OnExecute()
    {
        Console.WriteLine("MySubcommand executed");
    }
}
