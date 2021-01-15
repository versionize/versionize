using Xunit;
using LibGit2Sharp;
using Shouldly;
using System.Linq;
using Versionize.Tests.TestSupport;

namespace Versionize.Tests
{
    public class ChangelogazureLinkBuilderFactoryTests
    {
        [Fact]
        public void ShouldCreateAnAzureUrlBuilderForHTTPSPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", "https://dosse@dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP.git");
            var linkBuilder = ChangelogLinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
        }

        [Fact]
        public void ShouldCreateAnAzureUrlBuilderForSSHPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", "git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP.git");
            var linkBuilder = ChangelogLinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
        }

        [Fact]
        public void ShouldAzurePickFirstRemoteInCaseNoOriginWasFound()
        {
            var repo = SetupRepositoryWithRemote("some", "git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP.git");
            var linkBuilder = ChangelogLinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
        }

        [Fact]
        public void ShouldAzureFallbackToNoopInCaseNoGithubPushUrlWasDefined()
        {
            var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/saintedlama/versionize.git");
            var linkBuilder = ChangelogLinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<PlainLinkBuilder>();
        }

        private Repository SetupRepositoryWithRemote(string remoteName, string pushUrl)
        {
            var workingDirectory = TempDir.Create();
            var repo = TempRepository.Create(workingDirectory);

            foreach (var existingRemoteName in repo.Network.Remotes.Select(remote => remote.Name)) {
              repo.Network.Remotes.Remove(existingRemoteName);
            }

            repo.Network.Remotes.Add(remoteName, pushUrl);

            return repo;
        }
    }
}
