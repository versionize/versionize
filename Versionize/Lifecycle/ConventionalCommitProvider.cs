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

        if (options.AggregatePrereleases)
        {
            versionToUseForCommitDiff = repo.Tags
                .Select(options.Project.ExtractTagVersion)
                .Where(x => x != null && !x.IsPrerelease)
                .OrderDescending()
                .FirstOrDefault();
        }

        var isFirstRelease = false;
        IReadOnlyList<Commit> commitsInVersion;
        var commitFilter = new CommitFilter
        {
            FirstParentOnly = options.FirstParentOnlyCommits
        };

        if (options.FindReleaseCommitViaMessage)
        {
            var lastReleaseCommit = repo.GetCommits(options.Project, commitFilter).FirstOrDefault(x => x.Message.StartsWith("chore(release):"));
            isFirstRelease = lastReleaseCommit is null;
            commitsInVersion = repo.GetCommitsSinceLastReleaseCommit(options.Project, commitFilter);
        }
        else
        {
            var versionTag = repo.SelectVersionTag(versionToUseForCommitDiff, options.Project);
            isFirstRelease = versionTag == null;
            commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag, options.Project, commitFilter);
        }

        var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion, options.CommitParser);

        return new ConventionalCommitsResult(
            IsFirstRelease: isFirstRelease,
            ConventionalCommits: conventionalCommits);
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
