using LibGit2Sharp;

namespace Versionize.Changelog
{
    public abstract class LinkBuilderFactory
    {
        public static IChangelogLinkBuilder CreateFor(Repository repository)
        {
            var origin = repository.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin") ?? repository.Network.Remotes.FirstOrDefault();

            if (origin == null)
            {
                return new PlainLinkBuilder();
            }

            if (GithubLinkBuilder.IsPushUrl(origin.PushUrl))
            {
                return new GithubLinkBuilder(origin.PushUrl);
            }
            else if (AzureLinkBuilder.IsPushUrl(origin.PushUrl))
            {
                return new AzureLinkBuilder(origin.PushUrl);
            }
            else if (GitlabLinkBuilder.IsPushUrl(origin.PushUrl))
            {
                return new GitlabLinkBuilder(origin.PushUrl);
            }
            else if (BitbucketLinkBuilder.IsPushUrl(origin.PushUrl))
            {
                return new BitbucketLinkBuilder(origin.PushUrl);
            }

            return new PlainLinkBuilder();
        }
    }
}
