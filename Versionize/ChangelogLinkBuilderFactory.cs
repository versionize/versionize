using System;
using System.Linq;
using LibGit2Sharp;

namespace Versionize
{
    public abstract class ChangelogLinkBuilderFactory
    {
        public static IChangelogLinkBuilder CreateFor(Repository repository)
        {
            var origin = repository.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin") ?? repository.Network.Remotes.FirstOrDefault();

            if (origin != null && IsGithubPushUrl(origin.PushUrl))
            {
                return new GithubLinkBuilder(origin.PushUrl);
            }
            else if(origin != null && isAzurePushUrl(origin.PushUrl))
            {
                return new AzureLinkBuilder(origin.PushUrl);
            }

            return new PlainLinkBuilder();
        }

        private static bool IsGithubPushUrl(string pushUrl)
        {
            return pushUrl.StartsWith("git@github.com:") || pushUrl.StartsWith("https://github.com/");
        }

        private static bool isAzurePushUrl(string pushUrl)
        {
            return pushUrl.StartsWith("git@ssh.dev.azure.com:") || (pushUrl.StartsWith("https://") && pushUrl.Contains("@dev.azure.com/"));
        }
    }
}
