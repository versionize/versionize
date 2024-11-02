using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;

namespace Versionize.Git;

public static class RepositoryExtensions
{
    public static Tag? SelectVersionTag(this Repository repository, SemanticVersion version)
    {
        return SelectVersionTag(repository, version, ProjectOptions.DefaultOneProjectPerRepo);
    }

    public static Tag? SelectVersionTag(this Repository repository, SemanticVersion? version, ProjectOptions project)
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

    public static IEnumerable<Commit> GetCommits(this Repository repository, ProjectOptions project, CommitFilter? filter = null)
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

    public static List<Commit> GetCommitsSinceLastVersion(this Repository repository, Tag? versionTag, ProjectOptions project, CommitFilter? filter = null)
    {
        if (versionTag == null)
        {
            return repository.GetCommits(project, filter).ToList();
        }

        filter ??= new CommitFilter();
        filter.ExcludeReachableFrom = versionTag;

        return repository.GetCommits(project, filter).ToList();
    }

    public static List<Commit> GetCommitsSinceLastReleaseCommit(this Repository repository, ProjectOptions project, CommitFilter? filter = null)
    {
        var lastReleaseCommit = repository
            .GetCommits(project, filter)
            .FirstOrDefault(x => x.Message.StartsWith("chore(release):"));
        if (lastReleaseCommit == null)
        {
            return repository.Commits.ToList();
        }

        filter ??= new CommitFilter();
        filter.ExcludeReachableFrom = lastReleaseCommit;

        return repository.GetCommits(project, filter).ToList();
    }

    public static bool IsConfiguredForCommits(this Repository repository)
    {
        var name = repository.Config.Get<string>("user.name");
        var email = repository.Config.Get<string>("user.email");

        return name != null && email != null;
    }

    public static SemanticVersion? GetCurrentVersion(this Repository repository, VersionOptions options, IBumpFile bumpFile)
    {
        SemanticVersion? version;
        if (options.TagOnly)
        {
            version = repository.Tags
                .Select(options.Project.ExtractTagVersion)
                .Where(x => x != null)
                .OrderByDescending(x => x!.Major)
                .ThenByDescending(x => x!.Minor)
                .ThenByDescending(x => x!.Patch)
                .ThenByDescending(x => x!.Release)
                .FirstOrDefault();
        }
        else
        {
            version = bumpFile.Version;
        }

        return version;
    }
}

public sealed class VersionOptions
{
    public bool TagOnly { get; init; }
    public required ProjectOptions Project { get; init; }

    public static implicit operator VersionOptions(VersionizeOptions versionizeOptions)
    {
        return new VersionOptions
        {
            TagOnly = versionizeOptions.BumpFileType == BumpFileType.None,
            Project = versionizeOptions.Project,
        };
    }
}
