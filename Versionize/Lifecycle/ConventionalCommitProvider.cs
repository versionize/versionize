using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Git;

namespace Versionize;

public sealed class ConventionalCommitProvider
{    
    public static (bool, IReadOnlyList<ConventionalCommit>) GetCommits(Repository repo, Options options, SemanticVersion version)
    {
        var versionToUseForCommitDiff = version;

        if (options.AggregatePrereleases)
        {
            versionToUseForCommitDiff = repo
                .Tags
                .Select(options.Project.ExtractTagVersion)
                .Where(x => x != null && !x.IsPrerelease)
                .OrderByDescending(x => x.Major)
                .ThenByDescending(x => x.Minor)
                .ThenByDescending(x => x.Patch)
                .FirstOrDefault();
        }

        var isInitialRelease = false;
        List<Commit> commitsInVersion;
        if (options.UseCommitMessageInsteadOfTagToFindLastReleaseCommit)
        {
            var lastReleaseCommit = repo.GetCommits(options.Project).FirstOrDefault(x => x.Message.StartsWith("chore(release):"));
            isInitialRelease = lastReleaseCommit is null;
            commitsInVersion = repo.GetCommitsSinceLastReleaseCommit(options.Project);
        }
        else
        {
            var versionTag = repo.SelectVersionTag(versionToUseForCommitDiff, options.Project);
            isInitialRelease = versionTag == null;
            commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag, options.Project);
        }

        var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion, options.CommitParser);

        return (isInitialRelease, conventionalCommits);
    }
    
    public sealed class Options
    {
        public ProjectOptions Project { get; init; }
        public bool AggregatePrereleases { get; init; }
        public bool UseCommitMessageInsteadOfTagToFindLastReleaseCommit { get; init; }
        public CommitParserOptions CommitParser { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                AggregatePrereleases = versionizeOptions.AggregatePrereleases,
                CommitParser = versionizeOptions.CommitParser,
                Project = versionizeOptions.Project,
                UseCommitMessageInsteadOfTagToFindLastReleaseCommit = versionizeOptions.UseCommitMessageInsteadOfTagToFindLastReleaseCommit,
            };
        }
    }
}
