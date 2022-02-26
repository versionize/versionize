using LibGit2Sharp;
using NuGet.Versioning;

namespace Versionize;

public static class RespositoryExtensions
{
    public static Tag SelectVersionTag(this Repository repository, SemanticVersion version)
    {
        return repository.Tags.SingleOrDefault(t => t.FriendlyName == $"v{version}");
    }

    public static IEnumerable<Tag> VersionTags(this Repository repository)
    {
        return repository.Tags.Where(IsSemanticVersionTag);
    }

    public static bool VersionTagsExists(this Repository repository, SemanticVersion version)
    {
        return repository.VersionTags().Any(tag => tag.FriendlyName.Equals($"v{version}"));
    }

    public static bool IsSemanticVersionTag(this Tag tag)
    {
        if (string.IsNullOrWhiteSpace(tag.FriendlyName))
        {
            return false;
        }

        if (!tag.FriendlyName.StartsWith("v"))
        {
            return false;
        }

        return SemanticVersion.TryParse(tag.FriendlyName[1..], out SemanticVersion semanticVersion);
    }

    public static List<Commit> GetCommitsSinceLastVersion(this Repository repository, Tag versionTag)
    {
        if (versionTag == null)
        {
            return repository.Commits.ToList();
        }

        var filter = new CommitFilter
        {
            ExcludeReachableFrom = versionTag
        };

        return repository.Commits.QueryBy(filter).ToList();
    }

    public static bool IsConfiguredForCommits(this Repository repository)
    {
        var name = repository.Config.Get<string>("user.name");
        var email = repository.Config.Get<string>("user.email");

        return name != null && email != null;
    }
}
