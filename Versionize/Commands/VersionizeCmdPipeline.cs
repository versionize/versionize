using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
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
{
    // Steps
    private readonly IConventionalCommitProvider _conventionalCommitProvider;
    private readonly IVersionBumper _versionBumper;
    private readonly IBumpFileUpdater _bumpFileUpdater;
    private readonly IChangelogUpdater _changelogUpdater;
    private readonly IReleaseCommitter _releaseCommitter;
    private readonly IReleaseTagger _releaseTagger;

    // Results
    private VersionizeCmdContext? _context;
    private SemanticVersion? _originalVersion;
    private IBumpFile? BumpFile => _context?.BumpFile;
    private Changelog.ChangelogBuilder? _changelog;
    private VersionizeOptions Options => ThrowHelper.ThrowIfNull(_context?.Options);
    private Repository Repository => ThrowHelper.ThrowIfNull(_context?.Repository);
    private SemanticVersion NewVersion { get => ThrowHelper.ThrowIfNull(field); set; }
    private ConventionalCommitsResult ConventionalCommitsResult { get => ThrowHelper.ThrowIfNull(field); set; }

    public VersionizeCmdPipeline(
        IConventionalCommitProvider conventionalCommitProvider,
        IVersionBumper versionBumper,
        IBumpFileUpdater bumpFileUpdater,
        IChangelogUpdater changelogUpdater,
        IReleaseCommitter releaseCommitter,
        IReleaseTagger releaseTagger)
    {
        _conventionalCommitProvider = conventionalCommitProvider;
        _versionBumper = versionBumper;
        _bumpFileUpdater = bumpFileUpdater;
        _changelogUpdater = changelogUpdater;
        _releaseCommitter = releaseCommitter;
        _releaseTagger = releaseTagger;
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

        ConventionalCommitsResult = _conventionalCommitProvider.GetCommits(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep3 BumpVersion()
    {
        IVersionBumper.Input input = new()
        {
            IsFirstRelease = ConventionalCommitsResult.IsFirstRelease,
            OriginalVersion = _originalVersion,
            ConventionalCommits = ConventionalCommitsResult.ConventionalCommits,
        };

        NewVersion = _versionBumper.Bump(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep4 UpdateBumpFile()
    {
        IBumpFileUpdater.Input input = new()
        {
            NewVersion = NewVersion,
            BumpFile = BumpFile,
        };

        _bumpFileUpdater.Update(input, Options);
        return this;
    }

    public IVersionizeCmdPipeline.IStep5 UpdateChangelog()
    {
        IChangelogUpdater.Input input = new()
        {
            Repository = Repository,
            NewVersion = NewVersion,
            OriginalVersion = _originalVersion,
            ConventionalCommits = ConventionalCommitsResult.ConventionalCommits,
        };

        _changelog = _changelogUpdater.Update(input, Options);
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

        _releaseCommitter.CreateCommit(input, Options);
        return this;
    }

    public void CreateTag()
    {
        IReleaseTagger.Input input = new()
        {
            Repository = Repository,
            NewVersion = NewVersion,
        };

        _releaseTagger.CreateTag(input, Options);
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
