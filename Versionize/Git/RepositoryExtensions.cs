using LibGit2Sharp;
using NuGet.Versioning;

namespace Versionize;

public static class RespositoryExtensions
{
    public static Tag SelectVersionTag(this Repository repository, SemanticVersion version)
    {
        return SelectVersionTag(repository, version, ProjectOptions.DefaultOneProjectPerRepo);
    }

    public static Tag SelectVersionTag(this Repository repository, SemanticVersion version, ProjectOptions project)
    {
        if (version == null)
        {
            return null;
        }

        return repository.Tags.SingleOrDefault(t => t.FriendlyName.Equals(project.GetTagName(version)));
    }
    
    public static bool VersionTagsExists(this Repository repository, SemanticVersion version, ProjectOptions project)
    {
        var tagName = project.GetTagName(version);
        return repository.Tags.Any(tag => tag.FriendlyName.Equals(tagName));
    }

    public static bool IsSemanticVersionTag(this Tag tag)
    {
        return IsSemanticVersionTag(tag, ProjectOptions.DefaultOneProjectPerRepo);
    }

    public static bool IsSemanticVersionTag(this Tag tag, ProjectOptions project)
    {
        return project.ExtractTagVersion(tag) != null;
    }

    public static IEnumerable<Commit> GetCommits(this Repository repository, ProjectOptions project, CommitFilter filter = null)
    {
        if (!string.IsNullOrEmpty(project.Path))
        {
            filter ??= new CommitFilter();
            filter.SortBy = CommitSortStrategies.Time | CommitSortStrategies.Topological;

            return repository.Commits
                .QueryBy(project.Path, filter)
                .Select(x => x.Commit);
        }
        else
        {
            return filter != null
                ? repository.Commits.QueryBy(filter)
                : repository.Commits;
        }
    }

    public static List<Commit> GetCommitsSinceLastVersion(this Repository repository, Tag versionTag, ProjectOptions project)
    {
        if (versionTag == null)
        {
            return repository.GetCommits(project).ToList();
        }

        var filter = new CommitFilter
        {
            ExcludeReachableFrom = versionTag
        };
        
        return repository.GetCommits(project, filter).ToList();
    }

    public static List<Commit> GetCommitsSinceLastReleaseCommit(this Repository repository, ProjectOptions project)
    {
        var lastReleaseCommit = repository
            .GetCommits(project)
            .FirstOrDefault(x => x.Message.StartsWith("chore(release):"));
        if (lastReleaseCommit == null)
        {
            return repository.Commits.ToList();
        }

        var filter = new CommitFilter
        {
            ExcludeReachableFrom = lastReleaseCommit
        };

        return repository.GetCommits(project, filter).ToList();
    }

    public static bool IsConfiguredForCommits(this Repository repository)
    {
        var name = repository.Config.Get<string>("user.name");
        var email = repository.Config.Get<string>("user.email");

        return name != null && email != null;
    }
}
