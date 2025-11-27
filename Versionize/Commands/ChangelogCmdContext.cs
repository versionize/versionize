using LibGit2Sharp;
using Versionize.Config;
using Versionize.Git;

namespace Versionize.Commands;

internal interface IChangelogCmdContextProvider
{
    ChangelogCmdContext GetContext(string? versionStr, string? preamble);
}

internal sealed class ChangelogCmdContextProvider(
    IVersionizeOptionsProvider _optionsProvider,
    IRepositoryProvider _repositoryProvider) : IChangelogCmdContextProvider
{
    public ChangelogCmdContext GetContext(string? versionStr, string? preamble)
    {
        VersionizeOptions options = _optionsProvider.GetOptions();
        Repository repository = _repositoryProvider.GetRepository(options);

        var changelogOptions = new ChangelogCmdOptions
        {
            SkipBumpFile = options.SkipBumpFile,
            WorkingDirectory = options.WorkingDirectory,
            ProjectOptions = options.Project,
            FindReleaseCommitViaMessage = options.FindReleaseCommitViaMessage,
            FirstParentOnlyCommits = options.FirstParentOnlyCommits,
            CommitParser = options.CommitParser,
            AggregatePrereleases = options.AggregatePrereleases,
            VersionStr = versionStr,
            Preamble = preamble,
        };

        return new ChangelogCmdContext(
            changelogOptions,
            repository
        );
    }
}

internal sealed record ChangelogCmdContext(
    ChangelogCmdOptions Options,
    Repository Repository);

public sealed record ChangelogCmdOptions
{
    public required bool SkipBumpFile { get; init; }
    public required string WorkingDirectory { get; init; }
    public required ProjectOptions ProjectOptions { get; init; }
    public required bool FindReleaseCommitViaMessage { get; init; }
    public required bool FirstParentOnlyCommits { get; init; }
    public required CommitParserOptions CommitParser { get; init; }
    public required bool AggregatePrereleases { get; init; }
    public required string? VersionStr { get; init; }
    public required string? Preamble { get; init; }
}
