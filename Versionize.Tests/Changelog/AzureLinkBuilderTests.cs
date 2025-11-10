using LibGit2Sharp;
using Shouldly;
using Versionize.ConventionalCommits;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog;

public class AzureLinkBuilderTests
{
    [Fact]
    public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
    {
        Should.Throw<InvalidOperationException>(() => new AzureLinkBuilder("azure.com"));
    }

    [Fact]
    public void ShouldCreateAnAzureUrlBuilderForHTTPSPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://dosse@dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<AzureLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAnAzureUrlBuilderForSSHPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", "git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<AzureLinkBuilder>();
    }

    [Fact]
    public void ShouldAzurePickFirstRemoteInCaseNoOriginWasFound()
    {
        var repo = SetupRepositoryWithRemote("some", "git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<AzureLinkBuilder>();
    }

    [Fact]
    public void ShouldFallbackToNoopInCaseNoAzurePushUrlWasDefined()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");
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

        var linkBuilder = new AzureLinkBuilder("git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP.git");
        var link = linkBuilder.BuildCommitLink(commit);

        link.ShouldBe("https://dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP/commit/734713bc047d87bf7eac9674765ae793478c50d3");
    }

    [Fact]
    public void ShouldBuildAHTTPSCommitLink()
    {
        var commit = new ConventionalCommit
        {
            Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
        };

        var linkBuilder = new AzureLinkBuilder("https://dosse@dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP.git");
        var link = linkBuilder.BuildCommitLink(commit);

        link.ShouldBe("https://dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP/commit/734713bc047d87bf7eac9674765ae793478c50d3");
    }

    [Fact]
    public void ShouldBuildAHTTPSVersionTagLink()
    {
        var linkBuilder = new AzureLinkBuilder("https://dosse@dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP.git");
        var link = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");

        link.ShouldBe("https://dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP?version=GTv1.2.3");
    }

    [Fact]
    public void ShouldBuildASSHIssueLink()
    {
        var linkBuilder = new AzureLinkBuilder("git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP.git");
        var link = linkBuilder.BuildIssueLink("123");

        link.ShouldBe("https://dev.azure.com/dosse/DosSE.ERP.Cloud/_workitems/edit/123");
    }

    [Fact]
    public void ShouldBuildAHTTPSIssueLink()
    {
        var linkBuilder = new AzureLinkBuilder("https://dosse@dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP.git");
        var link = linkBuilder.BuildIssueLink("123");

        link.ShouldBe("https://dev.azure.com/dosse/DosSE.ERP.Cloud/_workitems/edit/123");
    }

    [Fact]
    public void ShouldCreateAnAzureUrlBuilderForSSHPushUrlsEvenWithoutGitSuffix()
    {
        var repo = SetupRepositoryWithRemote("origin", "git@ssh.dev.azure.com:v3/dosse/DosSE.ERP.Cloud/ERP");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<AzureLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAnAzureUrlBuilderForHTTPSPushUrlsEvenWithoutGitSuffix()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://dosse@dev.azure.com/dosse/DosSE.ERP.Cloud/_git/ERP");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<AzureLinkBuilder>();
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
