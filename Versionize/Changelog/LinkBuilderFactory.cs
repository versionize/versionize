using LibGit2Sharp;
using Versionize.Config;

namespace Versionize.Changelog;

public abstract class LinkBuilderFactory
{
    public static IChangelogLinkBuilder CreateFor(Repository repository, ChangelogLinkTemplates? linkTemplates = null)
    {
        var origin = repository.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin") ?? repository.Network.Remotes.FirstOrDefault();

        if (origin == null)
        {
            return new NullLinkBuilder();
        }

        IChangelogLinkBuilder linkBuilder = origin.PushUrl switch
        {
            var x when GithubLinkBuilder.IsPushUrl(x) => new GithubLinkBuilder(x),
            var x when AzureLinkBuilder.IsPushUrl(x) => new AzureLinkBuilder(x),
            var x when GitlabLinkBuilder.IsPushUrl(x) => new GitlabLinkBuilder(x),
            var x when BitbucketLinkBuilder.IsPushUrl(x) => new BitbucketLinkBuilder(x),
            _ => new NullLinkBuilder()
        };

        if (linkTemplates != null)
        {
            linkBuilder = new TemplatedLinkBuilder(linkTemplates, linkBuilder);
        }

        return linkBuilder;
    }
}
