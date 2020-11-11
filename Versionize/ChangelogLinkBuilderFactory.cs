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

            return new PlainLinkBuilder();
        }

        private static bool IsGithubPushUrl(string pushUrl)
        {
            return pushUrl.StartsWith("git@github.com:") || pushUrl.StartsWith("https://github.com/");
        }
    }
}
