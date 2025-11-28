using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Lifecycle;
using Versionize.Utils;

namespace Versionize.Commands;

internal sealed class VersionizeCmdPipeline :
    IVersionizeCmdPipeline,
    IVersionizeCmdPipeline.IStep1,
    IVersionizeCmdPipeline.IStep2,
    IVersionizeCmdPipeline.IStep3,
    IVersionizeCmdPipeline.IStep4,
    IVersionizeCmdPipeline.IStep5,
    IVersionizeCmdPipeline.IStep6
    // IParseCommitsSinceLastVersionPipeline,
    // IBumpVersionPipeline,
    // IUpdateBumpFilePipeline,
    // IUpdateChangelogPipeline,
    // ICreateCommitPipeline,
    // ICreateTagPipeline
{
    // Steps
    private readonly IConventionalCommitProvider _parseCommitsSinceLastVersionStep;
    private readonly IVersionBumper _bumpVersionStep;
    private readonly IBumpFileUpdater _updateBumpFileStep;
    private readonly IChangelogUpdater _updateChangelogStep;
    private readonly IReleaseCommitter _createCommitStep;
    private readonly IReleaseTagger _createTagStep;

    // Results
    private VersionizeCmdContext? _context;
    private SemanticVersion? _originalVersion;
    private Changelog.ChangelogBuilder? _changelog;
    // private LibGit2Sharp.Commit? _releaseCommit;
    // private LibGit2Sharp.Tag? _releaseTag;

    private VersionizeOptions Options => ThrowHelper.ThrowIfNull(_context?.Options);
    private Repository Repository => ThrowHelper.ThrowIfNull(_context?.Repository);
    private IBumpFile BumpFile => ThrowHelper.ThrowIfNull(_context?.BumpFile);
    private SemanticVersion NewVersion { get => ThrowHelper.ThrowIfNull(field); set; }
    //private Changelog.ChangelogBuilder Changelog { get => ThrowHelper.ThrowIfNull(field); set; }
    private IReadOnlyList<ConventionalCommit> ConventionalCommits { get => ThrowHelper.ThrowIfNull(field); set; }

    public VersionizeCmdPipeline(
        IConventionalCommitProvider parseCommitsSinceLastVersionStep,
        IVersionBumper bumpVersionStep,
        IBumpFileUpdater updateBumpFileStep,
        IChangelogUpdater updateChangelogStep,
        IReleaseCommitter createCommitStep,
        IReleaseTagger createTagStep)
    {
        _parseCommitsSinceLastVersionStep = parseCommitsSinceLastVersionStep;
        _bumpVersionStep = bumpVersionStep;
        _updateBumpFileStep = updateBumpFileStep;
        _updateChangelogStep = updateChangelogStep;
        _createCommitStep = createCommitStep;
        _createTagStep = createTagStep;
    }

    public IVersionizeCmdPipeline.IStep1 Begin(VersionizeCmdContext context)
    {
        _context = context;
        _originalVersion = context.GetCurrentVersion();
        return this;
    }

    public IVersionizeCmdPipeline.IStep2 ParseCommitsSinceLastVersion()
    {
        IConventionalCommitProvider.Input input = new()
        {
            Repository = Repository,
            Version = _originalVersion,
        };

        ConventionalCommits = _parseCommitsSinceLastVersionStep.GetCommits(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep3 BumpVersion()
    {
        IVersionBumper.Input input = new()
        {
            OriginalVersion = _originalVersion,
            ConventionalCommits = ConventionalCommits,
        };

        NewVersion = _bumpVersionStep.Bump(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep4 UpdateBumpFile()
    {
        IBumpFileUpdater.Input input = new()
        {
            NewVersion = NewVersion,
            BumpFile = BumpFile,
        };

        _updateBumpFileStep.Update(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep5 UpdateChangelog()
    {
        IChangelogUpdater.Input input = new()
        {
            Repository = Repository,
            NewVersion = NewVersion,
            OriginalVersion = _originalVersion,
            ConventionalCommits = ConventionalCommits,
        };

        _changelog = _updateChangelogStep.Update(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep6 CreateCommit()
    {
        IReleaseCommitter.Input input = new()
        {
            Repository = Repository,
            NewVersion = NewVersion,
            BumpFile = BumpFile,
            Changelog = _changelog,
        };

        //_releaseCommit = _createCommitStep.CreateCommit(input, Options);
        _createCommitStep.CreateCommit(input, Options);
        return this;
    }

    public void CreateTag()
    {
        IReleaseTagger.Input input = new()
        {
            Repository = Repository,
            NewVersion = NewVersion,
        };

        //_releaseTag = _createTagStep.CreateTag(input, Options);
        _createTagStep.CreateTag(input, Options);
    }
}

internal interface IVersionizeCmdPipeline
{
    IStep1 Begin(VersionizeCmdContext context);

    internal interface IStep1
    {
        IStep2 ParseCommitsSinceLastVersion();
    }
    internal interface IStep2
    {
        IStep3 BumpVersion();
    }
    internal interface IStep3
    {
        IStep4 UpdateBumpFile();
    }
    internal interface IStep4
    {
        IStep5 UpdateChangelog();
    }
    internal interface IStep5
    {
        IStep6 CreateCommit();
    }
    internal interface IStep6
    {
        void CreateTag();
    }
}
// internal interface IParseCommitsSinceLastVersionPipeline
// {
//     IBumpVersionPipeline ParseCommitsSinceLastVersion();
// }
// internal interface IBumpVersionPipeline
// {
//     IUpdateBumpFilePipeline BumpVersion();
// }
// internal interface IUpdateBumpFilePipeline
// {
//     IUpdateChangelogPipeline UpdateBumpFile();
// }
// internal interface IUpdateChangelogPipeline
// {
//     ICreateCommitPipeline UpdateChangelog();
// }
// internal interface ICreateCommitPipeline
// {
//     ICreateTagPipeline CreateCommit();
// }
// internal interface ICreateTagPipeline
// {
//     void CreateTag();
// }
