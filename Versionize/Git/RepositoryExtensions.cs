using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.CommandLine;
using Versionize.Commands;

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

    public static SemanticVersion? GetCurrentVersion(this Repository repository, VersionOptions options, IBumpFile bumpFile)
    {
        if (options.SkipBumpFile)
        {
            return repository.Tags
                .Select(options.Project.ExtractTagVersion)
                .Where(x => x is not null)
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }

        return bumpFile.Version;
    }

    public static SemanticVersion? GetPreviousVersion(this Repository repository, SemanticVersion version, ChangelogCmdOptions options)
    {
        var versionsEnumerable = repository.Tags
            .Select(options.ProjectOptions.ExtractTagVersion)
            .OfType<SemanticVersion>();

        if (options.AggregatePrereleases)
        {
            versionsEnumerable = versionsEnumerable.Where(x => x == version || !x.IsPrerelease);
        }

        var versions = versionsEnumerable
            .OrderByDescending(x => x)
            .ToArray();

        var versionIndex = Array.IndexOf(versions, version);
        return versionIndex == -1 || versionIndex == versions.Length - 1
            ? null
            : versions[versionIndex + 1];
    }

    public static (GitObject? FromRef, GitObject ToRef) GetCommitRange(this Repository repo, string? versionStr, ChangelogCmdOptions options)
    {
        if (string.IsNullOrEmpty(versionStr))
        {
            versionStr = repo.Tags
                .Select(options.ProjectOptions.ExtractTagVersion)
                .Where(x => x is not null)
                .OrderByDescending(x => x)
                .FirstOrDefault()?
                .ToNormalizedString();

            if (string.IsNullOrEmpty(versionStr))
            {
                throw new VersionizeException(ErrorMessages.NoVersionFound(), 1);
            }
        }

        if (SemanticVersion.TryParse(versionStr, out var version))
        {
            var toRef = repo.SelectVersionTag(version, options.ProjectOptions)?.Target;
            if (toRef is null)
            {
                var versionText = version.ToNormalizedString();
                throw new VersionizeException(ErrorMessages.TagForVersionNotFound(versionText), 1);
            }

            var fromVersion = repo.GetPreviousVersion(version, options);
            GitObject? fromRef = repo.SelectVersionTag(fromVersion, options.ProjectOptions)?.Target;

            return (fromRef, toRef);
        }

        throw new VersionizeException(ErrorMessages.InvalidVersionFormat(versionStr), 1);
    }

    // Variant that relies only on tags and local helpers (no external helpers, no bump files)
    public static (GitObject? FromRef, GitObject ToRef) GetCommitRangeFromTags(this IRepository repo, string? versionStr, VersionizeOptions options)
    {
        IEnumerable<SemanticVersion> AllVersions()
        {
            return repo.Tags
                .Select(options.Project.ExtractTagVersion)
                .OfType<SemanticVersion>()
                .OrderByDescending(v => v);
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
            var name = options.Project.GetTagName(v);
            return repo.Tags.SingleOrDefault(t => t.FriendlyName.Equals(name))?.Target;
        }

        SemanticVersion ResolveCurrent(string? text, IEnumerable<SemanticVersion> versions)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return ParseVersionOrThrow(text);
            }

            var latest = versions.FirstOrDefault();
            return latest ?? throw new VersionizeException(ErrorMessages.NoVersionFound(), 1);
        }

        static SemanticVersion? ResolvePrevious(
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
        var current = ResolveCurrent(versionStr, allVersions);

        var toRef = TagTargetFor(current)
            ?? throw new VersionizeException(ErrorMessages.TagForVersionNotFound(current.ToNormalizedString()), 1);

        var previous = ResolvePrevious(current, allVersions, options.AggregatePrereleases);
        var fromRef = previous is null ? null : TagTargetFor(previous);

        return (fromRef, toRef);
    }
}

public sealed class VersionOptions
{
    public bool SkipBumpFile { get; init; }
    public required ProjectOptions Project { get; init; }

    public static implicit operator VersionOptions(VersionizeOptions versionizeOptions)
    {
        return new VersionOptions
        {
            SkipBumpFile = versionizeOptions.SkipBumpFile,
            Project = versionizeOptions.Project,
        };
    }
}
