using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Commands;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Git;

using Input = Versionize.Lifecycle.IConventionalCommitProvider.Input;
using Options = Versionize.Lifecycle.IConventionalCommitProvider.Options;

namespace Versionize.Lifecycle;

public sealed class ConventionalCommitProvider : IConventionalCommitProvider
{
    public ConventionalCommitsResult GetCommits(Input input, Options options)
    {
        Repository repo = input.Repository;
        SemanticVersion? versionToUseForCommitDiff = input.Version;

        var (fromRef, toRef) = GetCommitRange(repo, version, options);
        var isFirstRelease = fromRef == null;
        var conventionalCommits = GetCommits(repo, options, fromRef, toRef);

        return new ConventionalCommitsResult(isFirstRelease, conventionalCommits);
    }

    private (GitObject? FromRef, GitObject ToRef) GetCommitRange(Repository repo, SemanticVersion? version, Options options)
    {
        if (version is null)
        {
            return (null, repo.Head.Tip);
        }

        if (options.FindReleaseCommitViaMessage)
        {
            var commitFilter = new CommitFilter
            {
                FirstParentOnly = options.FirstParentOnlyCommits,
            };

            var lastReleaseCommit = repo
                .GetCommits(options.Project, commitFilter)
                .FirstOrDefault(x => x.Message.StartsWith("chore(release):"));

            return (lastReleaseCommit, repo.Head.Tip);
        }

        if (version.IsPrerelease && options.AggregatePrereleases)
        {
            var tag = repo.Tags
                .Select(tag => (Tag: tag, Version: options.Project.ExtractTagVersion(tag)))
                .OrderByDescending(x => x.Version)
                .Where(x => x.Version is { IsPrerelease: false })
                .Select(x => x.Tag)
                .FirstOrDefault();

            return (tag?.Target, repo.Head.Tip);
        }

        var tagForVersion = repo.SelectVersionTag(version, options.Project);
        return (tagForVersion?.Target, repo.Head.Tip);
    }

    public static IReadOnlyList<ConventionalCommit> GetCommits(Repository repo, Options options, GitObject? fromRef, GitObject toRef)
    {
        var commitFilter = new CommitFilter
        {
            FirstParentOnly = options.FirstParentOnlyCommits,
            IncludeReachableFrom = toRef,
            ExcludeReachableFrom = fromRef,
        };

        IEnumerable<Commit> commitsInVersion = repo.GetCommits(options.Project, commitFilter);
        var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion, options.CommitParser);

        return conventionalCommits;
    }
}

public record ConventionalCommitsResult(
    bool IsFirstRelease,
    IReadOnlyList<ConventionalCommit> ConventionalCommits);

public interface IConventionalCommitProvider
{
    ConventionalCommitsResult GetCommits(Input input, Options options);

    sealed class Input
    {
        public required Repository Repository { get; init; }
        public required SemanticVersion? Version { get; init; }
    }

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

        public static implicit operator Options(ChangelogCmdOptions changelogCmdOptions)
        {
            return new Options
            {
                Project = changelogCmdOptions.ProjectOptions,
                AggregatePrereleases = changelogCmdOptions.AggregatePrereleases,
                FindReleaseCommitViaMessage = changelogCmdOptions.FindReleaseCommitViaMessage,
                FirstParentOnlyCommits = changelogCmdOptions.FirstParentOnlyCommits,
                CommitParser = changelogCmdOptions.CommitParser,
            };
        }
    }
}
