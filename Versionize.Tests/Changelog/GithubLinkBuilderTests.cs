using LibGit2Sharp;
using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog.Tests;

public class GithubLinkBuilderTests
{
    [Fact]
    public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
    {
        Should.Throw<InvalidOperationException>(() => new GithubLinkBuilder("github.com"));
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForHTTPSPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://github.com/saintedlama/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForSSHPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", "git@github.com:saintedlama/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
    {
        var repo = SetupRepositoryWithRemote("some", "git@github.com:saintedlama/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldFallbackToNoopInCaseNoGithubPushUrlWasDefined()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/saintedlama/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<PlainLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForSSHPushUrlsEvenWithoutGitSuffix()
    {
        var repo = SetupRepositoryWithRemote("origin", "git@github.com:saintedlama/versionize");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForHTTPSPushUrlsEvenWithoutGitSuffix()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://github.com/saintedlama/versionize");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
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
