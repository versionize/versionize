using System.Data;
using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
using Versionize.ConventionalCommits;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog.LinkBuilders;

public class BitBucketLinkBuilderTests : IDisposable
{
    private readonly string sshOrgPushUrl = "git@bitbucket.org:mobiloitteinc/dotnet-codebase.git";
    private readonly string sshComPushUrl = "git@bitbucket.com:mobiloitteinc/dotnet-codebase.git";
    private readonly string httpsOrgPushUrl = "https://versionize@bitbucket.org/mobiloitteinc/dotnet-codebase.git";
    private readonly string httpsComPushUrl = "https://versionize@bitbucket.com/mobiloitteinc/dotnet-codebase.git";

    private Repository _repo;

    [Fact]
    public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
    {
        Should.Throw<VersionizeException>(() => new BitbucketLinkBuilder("bitbucket.org"));
    }

    [Fact]
    public void ShouldThrowIfUrlIsNoValidHttpsCloneUrl()
    {
        Should.Throw<VersionizeException>(() => new BitbucketLinkBuilder("https://versionize@bitbucket.org/"));
    }

    [Fact]
    public void ShouldThrowIfUrlIsNoValidSshCloneUrl()
    {
        Should.Throw<VersionizeException>(() => new BitbucketLinkBuilder("git@bitbucket.org:mobiloitteinc"));
    }

    [Fact]
    public void ShouldCreateAnOrgBitbucketUrlBuilderForHTTPSPushUrls()
    {
        _repo = SetupRepositoryWithRemote("origin", httpsOrgPushUrl);
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAComBitbucketUrlBuilderForHTTPSPushUrls()
    {
        _repo = SetupRepositoryWithRemote("origin", httpsComPushUrl);
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAnOrgBitbucketUrlBuilderForSSHPushUrls()
    {
        _repo = SetupRepositoryWithRemote("origin", sshOrgPushUrl);
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAComBitbucketUrlBuilderForSSHPushUrls()
    {
        _repo = SetupRepositoryWithRemote("origin", sshComPushUrl);
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
    }

    [Fact]
    public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
    {
        _repo = SetupRepositoryWithRemote("some", sshOrgPushUrl);
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<BitbucketLinkBuilder>();
    }

    [Fact]
    public void ShouldFallbackToNoopInCaseNoBitbucketPushUrlWasDefined()
    {
        _repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<NullLinkBuilder>();
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
    public void ShouldBuildAnOrgSSHIssueLink()
    {
        var linkBuilder = new BitbucketLinkBuilder(sshOrgPushUrl);
        var link = linkBuilder.BuildIssueLink("123");

        link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/issues/123");
    }

    [Fact]
    public void ShouldBuildAComSSHIssueLink()
    {
        var linkBuilder = new BitbucketLinkBuilder(sshComPushUrl);
        var link = linkBuilder.BuildIssueLink("321");

        link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/issues/321");
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
        var link = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");

        link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/src/v1.2.3");
    }

    [Fact]
    public void ShouldBuildAComSSHTagLink()
    {
        var linkBuilder = new BitbucketLinkBuilder(sshComPushUrl);
        var link = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");

        link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/src/v1.2.3");
    }

    [Fact]
    public void ShouldBuildAnOrgHTTPSTagLink()
    {
        var linkBuilder = new BitbucketLinkBuilder(httpsOrgPushUrl);
        var link = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");

        link.ShouldBe("https://bitbucket.org/mobiloitteinc/dotnet-codebase/src/v1.2.3");
    }

    [Fact]
    public void ShouldBuildAComHTTPSTagLink()
    {
        var linkBuilder = new BitbucketLinkBuilder(httpsComPushUrl);
        var link = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");

        link.ShouldBe("https://bitbucket.com/mobiloitteinc/dotnet-codebase/src/v1.2.3");
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

    public void Dispose()
    {
        if (_repo is not null)
        {
            var workingDirectory = _repo.Info.WorkingDirectory;
            _repo.Dispose();
            Cleanup.DeleteDirectory(workingDirectory);
        }
    }
}
