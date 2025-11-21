using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Git;

namespace Versionize.Pipeline.VersionizeSteps;

// GetCommitsStep
// ParseCommitMessages
// ParseCommitsSinceLastVersion or ForNewVersion vs ParseCommitsForVersion
public class ParseCommitsSinceLastVersionStep : IPipelineStep<ReadVersionResult, ParseCommitsSinceLastVersionStep.Options, ParseCommitsSinceLastVersionResult>
{
    public ParseCommitsSinceLastVersionResult Execute(ReadVersionResult input, Options options)
    {
        var (isFirstRelease, commits) = GetCommits(input.Repository, options, input.Version);

        return new ParseCommitsSinceLastVersionResult
        {
            Repository = input.Repository,
            BumpFile = input.BumpFile,
            Version = input.Version,
            IsFirstRelease = isFirstRelease,
            Commits = commits,
        };
    }

    private static (bool, IReadOnlyList<ConventionalCommit>) GetCommits(Repository repo, Options options, SemanticVersion? version)
    {
        var versionToUseForCommitDiff = version;

        if (options.AggregatePrereleases)
        {
            versionToUseForCommitDiff = repo.Tags
                .Select(options.Project.ExtractTagVersion)
                .Where(x => x != null && !x.IsPrerelease)
                .OrderByDescending(x => x!.Major)
                .ThenByDescending(x => x!.Minor)
                .ThenByDescending(x => x!.Patch)
                .FirstOrDefault();
        }

        var isInitialRelease = false;
        List<Commit> commitsInVersion;
        var commitFilter = new CommitFilter
        {
            FirstParentOnly = options.FirstParentOnlyCommits
        };

        if (options.FindReleaseCommitViaMessage)
        {
            var lastReleaseCommit = repo.GetCommits(options.Project, commitFilter).FirstOrDefault(x => x.Message.StartsWith("chore(release):"));
            isInitialRelease = lastReleaseCommit is null;
            commitsInVersion = repo.GetCommitsSinceLastReleaseCommit(options.Project, commitFilter);
        }
        else
        {
            var versionTag = repo.SelectVersionTag(versionToUseForCommitDiff, options.Project);
            isInitialRelease = versionTag == null;
            commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag, options.Project, commitFilter);
        }

        var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion, options.CommitParser);

        return (isInitialRelease, conventionalCommits);
    }

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public required ProjectOptions Project { get; init; }
        public bool AggregatePrereleases { get; init; }
        public bool FindReleaseCommitViaMessage { get; init; }
        public bool FirstParentOnlyCommits { get; init; }
        public required CommitParserOptions CommitParser { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                AggregatePrereleases = versionizeOptions.AggregatePrereleases,
                CommitParser = versionizeOptions.CommitParser,
                Project = versionizeOptions.Project,
                FindReleaseCommitViaMessage = versionizeOptions.FindReleaseCommitViaMessage,
                FirstParentOnlyCommits = versionizeOptions.FirstParentOnlyCommits,
            };
        }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
