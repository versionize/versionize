using LibGit2Sharp;
using NuGet.Versioning;
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

    [Theory]
    [InlineData(
        "",
        "https://www.github.com/myOrg/myRepo/releases/tag/v1.2.3")]
    [InlineData(
        null,
        "https://www.github.com/myOrg/myRepo/releases/tag/v1.2.3")]
    [InlineData(
        "https://www.github.com/{{owner}}/{{repository}}/compare/{{previousTag}}...{{currentTag}}",
        "https://www.github.com/myOrg/myRepo/compare/v1.2.2...v1.2.3")]
    public void ShouldBuildVersionTagLink(string compareUrlFormat, string expected)
    {
        SemanticVersion newVersion = SemanticVersion.Parse("1.2.3");
        SemanticVersion previousVersion = SemanticVersion.Parse("1.2.2");

        var linkBuilder = new GithubLinkBuilder("https://github.com/myOrg/myRepo.git");
        var link = linkBuilder.BuildVersionTagLink(newVersion, previousVersion, compareUrlFormat);

        link.ShouldBe(expected);
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
