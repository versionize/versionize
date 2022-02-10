using LibGit2Sharp;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize;

public static class RespositoryExtensions
{
    public static Tag SelectVersionTag(this Repository repository, Version version)
    {
        return repository.Tags.SingleOrDefault(t => t.FriendlyName == $"v{version}");
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
