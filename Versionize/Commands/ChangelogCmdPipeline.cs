using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Git;
using Versionize.Utils;

namespace Versionize.Commands;

internal sealed class ChangelogCmdPipeline :
    IChangelogCmdPipeline,
    IChangelogCmdPipeline.IRefRangeFinder,
    IChangelogCmdPipeline.ICommitParser,
    IChangelogCmdPipeline.IChangeListGenerator
{
    // Results
    private ChangelogCmdContext? _context;
    private string? VersionStr => ThrowHelper.ThrowIfNull(_context?.Options.VersionStr);
    private (GitObject? FromRef, GitObject ToRef)? RefRange { get => ThrowHelper.ThrowIfNull(field); set; }

    //private VersionizeOptions Options => ThrowHelper.ThrowIfNull(_context?.Options);
    private IRepository Repository => ThrowHelper.ThrowIfNull(_context?.Repository);
    private IReadOnlyList<ConventionalCommit> ConventionalCommits { get => ThrowHelper.ThrowIfNull(field); set; }

    public ChangelogCmdPipeline()
    {
    }

    public IChangelogCmdPipeline.IRefRangeFinder Begin(ChangelogCmdContext context)
    {
        _context = context;
        return this;
    }

    public IChangelogCmdPipeline.ICommitParser FindRefRange()
    {
        //var (FromRef, ToRef) = Repository.GetCommitRange(VersionStr, _context.Options);
        return this;
    }

    public IChangelogCmdPipeline.IChangeListGenerator ParseCommits()
    {
        // ConventionalCommitProvider.GetCommits
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
