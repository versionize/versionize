using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Git;
using Versionize.Lifecycle;
using Versionize.Utils;

namespace Versionize.Commands;

internal sealed class ChangelogCmdPipeline :
    IChangelogCmdPipeline,
    IChangelogCmdPipeline.IRefRangeFinder,
    IChangelogCmdPipeline.ICommitParser,
    IChangelogCmdPipeline.IChangeListGenerator
{
    // Steps
    private readonly ConventionalCommitProvider _conventionalCommitProvider;

    // Results
    private ChangelogCmdContext? _context;
    private string? VersionStr => ThrowHelper.ThrowIfNull(_context?.Options.VersionStr);
    private (GitObject? FromRef, GitObject ToRef)? RefRange { get => ThrowHelper.ThrowIfNull(field); set; }

    private ChangelogCmdOptions Options => ThrowHelper.ThrowIfNull(_context?.Options);
    private Repository Repository => ThrowHelper.ThrowIfNull(_context?.Repository);
    private IReadOnlyList<ConventionalCommit> ConventionalCommits { get => ThrowHelper.ThrowIfNull(field); set; }

    public ChangelogCmdPipeline(
        ConventionalCommitProvider conventionalCommitProvider
    )
    {
        _conventionalCommitProvider = conventionalCommitProvider;
    }

    public IChangelogCmdPipeline.IRefRangeFinder Begin(ChangelogCmdContext context)
    {
        _context = context;
        return this;
    }

    public IChangelogCmdPipeline.ICommitParser FindRefRange()
    {
        //RefRange = Repository.GetCommitRange(VersionStr, Options);
        return this;
    }

    public IChangelogCmdPipeline.IChangeListGenerator ParseCommits()
    {
        // ConventionalCommits = ConventionalCommitProvider.GetCommits(
        //     Repository,
        //     Options,
        //     RefRange.Value.FromRef,
        //     RefRange.Value.ToRef);
        return this;
    }

    public string GenerateChangeList()
    {
        // ChangelogUpdater.Update / changelog.Write
        return string.Empty;
    }
}

internal interface IChangelogCmdPipeline
{
    IRefRangeFinder Begin(ChangelogCmdContext context);

    internal interface IRefRangeFinder
    {
        ICommitParser FindRefRange();
    }

    internal interface ICommitParser
    {
        IChangeListGenerator ParseCommits();
    }

    internal interface IChangeListGenerator
    {
        string GenerateChangeList();
    }
}
