using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
using Versionize.ConventionalCommits;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog;

public class GithubLinkBuilderTests
{
    [Fact]
    public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
    {
        Should.Throw<VersionizeException>(() => new GithubLinkBuilder("github.com"));
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForHTTPSPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://github.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForSSHPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", "git@github.com:versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
    {
        var repo = SetupRepositoryWithRemote("some", "git@github.com:versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldFallbackToNoopInCaseNoGithubPushUrlWasDefined()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<PlainLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForSSHPushUrlsEvenWithoutGitSuffix()
    {
        var repo = SetupRepositoryWithRemote("origin", "git@github.com:versionize/versionize");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForHTTPSPushUrlsEvenWithoutGitSuffix()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://github.com/versionize/versionize");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldBuildASSHLink()
    {
        var linkBuilder = new GithubLinkBuilder("git@github.com:versionize/versionize");

        linkBuilder.BuildIssueLink("123")
            .ShouldBe("https://www.github.com/versionize/versionize/issues/123");
        linkBuilder.BuildCommitLink(
                new ConventionalCommit
                {
                    Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
                })
            .ShouldBe("https://www.github.com/versionize/versionize/commit/734713bc047d87bf7eac9674765ae793478c50d3");
        linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2")
            .ShouldBe("https://www.github.com/versionize/versionize/releases/tag/v1.2.3");
    }

    [Fact]
    public void ShouldBuildAHTTPSLink()
    {
        var linkBuilder = new GithubLinkBuilder("https://github.com/versionize/versionize.git");

        linkBuilder.BuildIssueLink("123")
            .ShouldBe("https://www.github.com/versionize/versionize/issues/123");
        linkBuilder.BuildCommitLink(
                new ConventionalCommit
                {
                    Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
                })
            .ShouldBe("https://www.github.com/versionize/versionize/commit/734713bc047d87bf7eac9674765ae793478c50d3");
        linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2")
            .ShouldBe("https://www.github.com/versionize/versionize/releases/tag/v1.2.3");
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
