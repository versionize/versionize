using System;
using System.Data;
using System.Linq;
using LibGit2Sharp;
using NuGet.Versioning;
using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog.Tests
{
    public class BitBucketLinkBuilderTests
    {
        private readonly string sshPushUrl = "git@bitbucket.org:mobiloitteinc/dotnet-codebase.git";
        private readonly string httpsPushUrl = "https://saintedlama@bitbucket.org/mobiloitteinc/dotnet-codebase.git";

        [Fact]
        public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
        {
            Should.Throw<InvalidOperationException>(() => new BitbucketLinkBuilder("bitbucket.org"));
        }

        [Fact]
        public void ShouldThrowIfUrlIsNoValidHttpsCloneUrl()
        {
            Should.Throw<InvalidOperationException>(() => new BitbucketLinkBuilder("https://saintedlama@bitbucket.org/"));
        }

        [Fact]
        public void ShouldThrowIfUrlIsNoValidSshCloneUrl()
        {
            Should.Throw<InvalidOperationException>(() => new BitbucketLinkBuilder("git@bitbucket.org:mobiloitteinc"));
        }

        [Fact]
        public void ShouldCreateABitbucketUrlBuilderForHTTPSPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", httpsPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldCreateABitbucketUrlBuilderForSSHPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", sshPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
        {
            var repo = SetupRepositoryWithRemote("some", sshPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldFallbackToNoopInCaseNoBitbucketPushUrlWasDefined()
        {
            var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/saintedlama/versionize.git");
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<PlainLinkBuilder>();
        }

        [Fact]
        public void ShouldBuildASSHCommitLink()
        {
            var commit = new ConventionalCommit
            {
                Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
            };

            var linkBuilder = new BitbucketLinkBuilder(sshPushUrl);
            var link = linkBuilder.BuildCommitLink(commit);

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        }

        [Fact]
        public void ShouldBuildAHTTPSCommitLink()
        {
            var commit = new ConventionalCommit
            {
                Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
            };

            var linkBuilder = new BitbucketLinkBuilder(httpsPushUrl);
            var link = linkBuilder.BuildCommitLink(commit);

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        }

        [Fact]
        public void ShouldBuildASSHTagLink()
        {
            var linkBuilder = new BitbucketLinkBuilder(sshPushUrl);
            var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0));

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/src/v1.0.0");
        }

        [Fact]
        public void ShouldBuildAnHTTPSTagLink()
        {
            var linkBuilder = new BitbucketLinkBuilder(httpsPushUrl);
            var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0));

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/src/v1.0.0");
        }

        private static Repository SetupRepositoryWithRemote(string remoteName, string pushUrl)
        {
            var workingDirectory = TempDir.Create();
            var repo = TempRepository.Create(workingDirectory);

            foreach (var existingRemoteName in repo.Network.Remotes.Select(remote => remote.Name))
            {
                repo.Network.Remotes.Remove(existingRemoteName);
            }

            repo.Network.Remotes.Add(remoteName, pushUrl);

            return repo;
        }
    }
}
