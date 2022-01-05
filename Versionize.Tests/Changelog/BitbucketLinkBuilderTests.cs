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
        private readonly string sshOrgPushUrl = "git@bitbucket.org:mobiloitteinc/dotnet-codebase.git";
        private readonly string sshComPushUrl = "git@bitbucket.com:mobiloitteinc/dotnet-codebase.git";
        private readonly string httpsOrgPushUrl = "https://saintedlama@bitbucket.org/mobiloitteinc/dotnet-codebase.git";
        private readonly string httpsComPushUrl = "https://saintedlama@bitbucket.com/mobiloitteinc/dotnet-codebase.git";

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
        public void ShouldCreateAnOrgBitbucketUrlBuilderForHTTPSPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", httpsOrgPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldCreateAComBitbucketUrlBuilderForHTTPSPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", httpsComPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldCreateAnOrgBitbucketUrlBuilderForSSHPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", sshOrgPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldCreateAComBitbucketUrlBuilderForSSHPushUrls()
        {
            var repo = SetupRepositoryWithRemote("origin", sshComPushUrl);
            var linkBuilder = LinkBuilderFactory.CreateFor(repo);

            linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
        }

        [Fact]
        public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
        {
            var repo = SetupRepositoryWithRemote("some", sshOrgPushUrl);
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
        public void ShouldBuildAnOrgSSHCommitLink()
        {
            var commit = new ConventionalCommit
            {
                Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
            };

            var linkBuilder = new BitbucketLinkBuilder(sshOrgPushUrl);
            var link = linkBuilder.BuildCommitLink(commit);

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        }

        [Fact]
        public void ShouldBuildAComSSHCommitLink()
        {
            var commit = new ConventionalCommit
            {
                Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
            };

            var linkBuilder = new BitbucketLinkBuilder(sshComPushUrl);
            var link = linkBuilder.BuildCommitLink(commit);

            link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        }

        [Fact]
        public void ShouldBuildAnOrgHTTPSCommitLink()
        {
            var commit = new ConventionalCommit
            {
                Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
            };

            var linkBuilder = new BitbucketLinkBuilder(httpsOrgPushUrl);
            var link = linkBuilder.BuildCommitLink(commit);

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        }

        [Fact]
        public void ShouldBuildAComHTTPSCommitLink()
        {
            var commit = new ConventionalCommit
            {
                Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
            };

            var linkBuilder = new BitbucketLinkBuilder(httpsComPushUrl);
            var link = linkBuilder.BuildCommitLink(commit);

            link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        }

        [Fact]
        public void ShouldBuildAnOrgSSHTagLink()
        {
            var linkBuilder = new BitbucketLinkBuilder(sshOrgPushUrl);
            var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0));

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/src/v1.0.0");
        }

        [Fact]
        public void ShouldBuildAComSSHTagLink()
        {
            var linkBuilder = new BitbucketLinkBuilder(sshComPushUrl);
            var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0));

            link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/src/v1.0.0");
        }

        [Fact]
        public void ShouldBuildAnOrgHTTPSTagLink()
        {
            var linkBuilder = new BitbucketLinkBuilder(httpsOrgPushUrl);
            var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0));

            link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/src/v1.0.0");
        }

        [Fact]
        public void ShouldBuildAComHTTPSTagLink()
        {
            var linkBuilder = new BitbucketLinkBuilder(httpsComPushUrl);
            var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0));

            link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/src/v1.0.0");
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
