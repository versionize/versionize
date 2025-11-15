using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.Lifecycle;
using Versionize.CommandLine;

namespace Versionize.Git;

public static class RepositoryExtensions
{
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
            return [.. repository.GetCommits(project, filter)];
        }

        filter ??= new CommitFilter();
        filter.ExcludeReachableFrom = versionTag;

        return [.. repository.GetCommits(project, filter)];
    }

    public static List<Commit> GetCommitsSinceLastReleaseCommit(this Repository repository, ProjectOptions project, CommitFilter? filter = null)
    {
        var lastReleaseCommit = repository
            .GetCommits(project, filter)
            .FirstOrDefault(x => x.Message.StartsWith("chore(release):"));

        if (lastReleaseCommit == null)
        {
            return [.. repository.Commits];
        }

        filter ??= new CommitFilter();
        filter.ExcludeReachableFrom = lastReleaseCommit;

        return [.. repository.GetCommits(project, filter)];
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
                .Where(x => x is not null)
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }
        else
        {
            version = bumpFile.Version;
        }

        return version;
    }

    public static SemanticVersion? GetPreviousVersion(this Repository repository, SemanticVersion version, VersionizeOptions options)
    {
        var versionsEnumerable = repository.Tags
            .Select(options.Project.ExtractTagVersion)
            .Where(x => x is not null);

        if (options.AggregatePrereleases)
        {
            versionsEnumerable = versionsEnumerable.Where(x => x == version || !x!.IsPrerelease);
        }

        var versions = versionsEnumerable
            .OrderByDescending(x => x)
            .ToArray();

        var versionIndex = Array.IndexOf(versions, version);
        return versionIndex == -1 || versionIndex == versions.Length - 1 ? null : versions[versionIndex + 1];
    }

    public static (GitObject? FromRef, GitObject ToRef) GetCommitRange(this Repository repo, string? versionStr, VersionizeOptions options)
    {
        if (string.IsNullOrEmpty(versionStr))
        {
            versionStr = repo.GetCurrentVersion(options, BumpFileProvider.GetBumpFile(options))?.ToNormalizedString();
            if (string.IsNullOrEmpty(versionStr))
            {
                throw new VersionizeException(ErrorMessages.NoVersionFound(), 1);
            }
        }

        if (SemanticVersion.TryParse(versionStr, out var version))
        {
            var toRef = repo.SelectVersionTag(version, options.Project)?.Target;
            if (toRef is null)
            {
                var versionText = version.ToNormalizedString();
                throw new VersionizeException(ErrorMessages.TagForVersionNotFound(versionText), 1);
            }

            var fromVersion = repo.GetPreviousVersion(version, options);
            GitObject? fromRef = repo.SelectVersionTag(fromVersion, options.Project)?.Target;

            return (fromRef, toRef);
        }

        throw new VersionizeException(ErrorMessages.InvalidVersionFormat(versionStr), 1);
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
