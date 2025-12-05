using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.CommandLine;
using Versionize.Commands;
using System.Linq;

namespace Versionize.Git;

public record struct CommitRange(GitObject? FromRef, GitObject ToRef);
public record struct VersionTag(SemanticVersion Version, Tag Tag);

public sealed class VersionedRepository
{
    public Repository Repository { get; }
    public ProjectOptions ProjectOptions { get; }

    public VersionedRepository(Repository repository, ProjectOptions projectOptions)
    {
        Repository = repository;
        ProjectOptions = projectOptions;
    }

    public bool TagExists(SemanticVersion version)
    {
        var tagName = ProjectOptions.GetTagName(version);
        return Repository.Tags[tagName] != null;
    }

    public Tag? GetTag(SemanticVersion? version)
    {
        if (version == null)
        {
            return null;
        }

        var tagName = ProjectOptions.GetTagName(version);
        return Repository.Tags[tagName];
    }

    public IEnumerable<Commit> GetCommits(CommitFilter? filter = null)
    {
        filter ??= new CommitFilter();

        if (string.IsNullOrEmpty(ProjectOptions.Path))
        {
            return Repository.Commits.QueryBy(filter);
        }

        filter.SortBy = CommitSortStrategies.Time | CommitSortStrategies.Topological;

        return Repository.Commits
            .QueryBy(ProjectOptions.Path, filter)
            .Select(x => x.Commit);
    }

    public IEnumerable<VersionTag> GetVersionTags()
    {
        foreach (var tag in Repository.Tags)
        {
            var version = ProjectOptions.ExtractTagVersion(tag);
            if (version is not null)
            {
                yield return new VersionTag(version, tag);
            }
        }
    }

    public VersionTag? GetLatestVersionTag()
    {
        var versionTags = GetVersionTags();
        if (!versionTags.Any())
        {
            return null;
        }

        return versionTags.MaxBy(vt => vt.Version);
    }

    public SemanticVersion? GetPreviousVersion(SemanticVersion version, bool aggregatePrereleases)
    {
        SemanticVersion? previous = null;

        foreach (var versionTag in GetVersionTags())
        {
            var current = versionTag.Version;

            if (aggregatePrereleases && current.IsPrerelease)
            {
                continue;
            }

            if (current < version && (previous == null || current > previous))
            {
                previous = current;
            }
        }

        return previous;
    }

    public VersionTag? GetPreviousVersionTag(SemanticVersion version, bool aggregatePrereleases)
    {
        VersionTag? previous = null;

        foreach (var versionTag in GetVersionTags())
        {
            var current = versionTag.Version;

            if (aggregatePrereleases && current.IsPrerelease)
            {
                continue;
            }

            if (current < version && (previous == null || current > previous.Value.Version))
            {
                previous = versionTag;
            }
        }

        return previous;
    }

    public GitObject? GetReleaseCommitViaMessage(SemanticVersion version)
    {
        var commitFilter = new CommitFilter
        {
            FirstParentOnly = true,
        };

        var expectedPrefix = $"chore(release): {version}";

        var lastReleaseCommit = Repository
            .GetCommits(ProjectOptions, commitFilter)
            .FirstOrDefault(x => x.Message.StartsWith(expectedPrefix));

        return lastReleaseCommit;
    }

    public GitObject? GetPreviousReleaseCommitViaMessage(GitObject toRef, bool aggregatePrereleases)
    {
        var commitFilter = new CommitFilter
        {
            FirstParentOnly = true,
            IncludeReachableFrom = toRef,
        };

        var commits = Repository
            .GetCommits(ProjectOptions, commitFilter)
            .Skip(1); // Skip the current release commit

        foreach (var commit in commits)
        {
            if (!commit.Message.StartsWith("chore(release):"))
            {
                continue;
            }

            if (!aggregatePrereleases)
            {
                return commit;
            }

            var messageParts = commit.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (messageParts.Length >= 2 &&
                SemanticVersion.TryParse(messageParts[1], out var version) &&
                version.IsPrerelease)
            {
                continue;
            }

            return commit;
        }

        return null;
    }
}

public static class RepositoryExtensions
{
    public static Tag? SelectVersionTag(this Repository repository, SemanticVersion? version, ProjectOptions project)
    {
        if (version == null)
        {
            return null;
        }

        var tagName = project.GetTagName(version);
        return repository.Tags[tagName];
        //return repository.Tags.SingleOrDefault(t => t.FriendlyName.Equals(tagName));
    }

    public static bool VersionTagsExists(this Repository repository, SemanticVersion version, ProjectOptions project)
    {
        var tagName = project.GetTagName(version);
        return repository.Tags[tagName] != null;
        //return repository.Tags.Any(tag => tag.FriendlyName.Equals(tagName));
    }

    public static IEnumerable<Commit> GetCommits(this Repository repository, ProjectOptions project, CommitFilter? filter = null)
    {
        filter ??= new CommitFilter();

        if (string.IsNullOrEmpty(project.Path))
        {
            return repository.Commits.QueryBy(filter);
        }

        filter.SortBy = CommitSortStrategies.Time | CommitSortStrategies.Topological;

        return repository.Commits
            .QueryBy(project.Path, filter)
            .Select(x => x.Commit);
    }

    public static IEnumerable<Commit> GetCommitsSinceRef(this Repository repository, GitObject? gitRef, ProjectOptions project, CommitFilter? filter = null)
    {
        if (gitRef == null)
        {
            return repository.GetCommits(project, filter);
        }

        filter ??= new CommitFilter();
        filter.ExcludeReachableFrom = gitRef;

        return repository.GetCommits(project, filter);
    }

    // Variant that relies only on tags and local helpers (no external helpers, no bump files)
    public static (GitObject? FromRef, GitObject ToRef) GetCommitRangeFromTags(this IRepository repo, string? versionStr, VersionizeOptions options)
    {
        IEnumerable<SemanticVersion> AllVersions()
        {
            return repo.Tags
                .Select(options.Project.ExtractTagVersion)
                .OfType<SemanticVersion>()
                .OrderDescending();
        }

        static SemanticVersion ParseVersionOrThrow(string text)
        {
            if (SemanticVersion.TryParse(text, out var v))
            {
                return v;
            }

            throw new VersionizeException(ErrorMessages.InvalidVersionFormat(text), 1);
        }

        GitObject? TagTargetFor(SemanticVersion v)
        {
            var tagName = options.Project.GetTagName(v);
            return repo.Tags.SingleOrDefault(t => t.FriendlyName.Equals(tagName))?.Target;
        }

        SemanticVersion ResolveTargetVersion(string? versionStr, IEnumerable<SemanticVersion> versions)
        {
            if (!string.IsNullOrEmpty(versionStr))
            {
                return ParseVersionOrThrow(versionStr);
            }

            var latest = versions.FirstOrDefault();
            return latest ?? throw new VersionizeException(ErrorMessages.NoVersionFound(), 1);
        }

        static SemanticVersion? ResolvePreviousVersion(
            SemanticVersion current,
            IEnumerable<SemanticVersion> all,
            bool aggregatePrereleases)
        {
            var seq = aggregatePrereleases
                ? all.Where(v => v == current || !v.IsPrerelease)
                : all;

            return seq
                .SkipWhile(v => v != current)
                .Skip(1)
                .FirstOrDefault();
        }

        var allVersions = AllVersions();
        var targetVersion = ResolveTargetVersion(versionStr, allVersions);

        var toRef = TagTargetFor(targetVersion)
            ?? throw new VersionizeException(ErrorMessages.TagForVersionNotFound(targetVersion.ToNormalizedString()), 1);

        var previousVersion = ResolvePreviousVersion(targetVersion, allVersions, options.AggregatePrereleases);
        var fromRef = previousVersion is null ? null : TagTargetFor(previousVersion);

        return (fromRef, toRef);
    }
}
