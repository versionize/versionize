using System.Data;
using LibGit2Sharp;
using NuGet.Versioning;
using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog.Tests;

public class GitlabLinkBuilderTests
{
    private readonly string inkscapeSSH = "git@gitlab.com:inkscape/inkscape.git";
    private readonly string inkscapeHTTPS = "https://gitlab.com/inkscape/inkscape.git";

    [Fact]
    public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
    {
        Should.Throw<InvalidOperationException>(() => new GitlabLinkBuilder("gitlab.com"));
    }

    [Fact]
    public void ShouldThrowIfUrlIsNoValidHttpsCloneUrl()
    {
        Should.Throw<InvalidOperationException>(() => new GitlabLinkBuilder("https://gitlab.com/inkscape"));
    }

    [Fact]
    public void ShouldThrowIfUrlIsNoValidSshCloneUrl()
    {
        Should.Throw<InvalidOperationException>(() => new GitlabLinkBuilder("git@gitlab.com:inkscape"));
    }

    [Fact]
    public void ShouldCreateAGitlabUrlBuilderForHTTPSPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", inkscapeHTTPS);
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GitlabLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGitlabUrlBuilderForSSHPushUrls()
    {
        var repo = SetupRepositoryWithRemote("origin", inkscapeSSH);
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GitlabLinkBuilder>();
    }

    [Fact]
    public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
    {
        var repo = SetupRepositoryWithRemote("some", inkscapeSSH);
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<GitlabLinkBuilder>();
    }

    [Fact]
    public void ShouldFallbackToNoopInCaseNoGitlabPushUrlWasDefined()
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

        var linkBuilder = new GitlabLinkBuilder(inkscapeSSH);
        var link = linkBuilder.BuildCommitLink(commit);

        link.ShouldBe("https://gitlab.com/inkscape/inkscape/-/commit/734713bc047d87bf7eac9674765ae793478c50d3");
    }

    [Fact]
    public void ShouldBuildAHTTPSCommitLink()
    {
        var commit = new ConventionalCommit
        {
            Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
        };

        var linkBuilder = new GitlabLinkBuilder(inkscapeHTTPS);
        var link = linkBuilder.BuildCommitLink(commit);

        link.ShouldBe("https://gitlab.com/inkscape/inkscape/-/commit/734713bc047d87bf7eac9674765ae793478c50d3");
    }

    [Fact]
    public void ShouldBuildASSHTagLink()
    {
        var linkBuilder = new GitlabLinkBuilder(inkscapeSSH);
        var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0), new SemanticVersion(0, 1, 0), null);

        link.ShouldBe("https://gitlab.com/inkscape/inkscape/-/tags/v1.0.0");
    }

    [Fact]
    public void ShouldBuildAnHTTPSTagLink()
    {
        var linkBuilder = new GitlabLinkBuilder(inkscapeHTTPS);
        var link = linkBuilder.BuildVersionTagLink(new SemanticVersion(1, 0, 0), new SemanticVersion(0, 1, 0), null);

        link.ShouldBe("https://gitlab.com/inkscape/inkscape/-/tags/v1.0.0");
    }

    [Theory]
    [InlineData(
        "",
        "https://gitlab.com/myOrg/myRepo/-/tags/v1.2.3")]
    [InlineData(
        "https://www.gitlab.com/{{owner}}/{{repository}}/compare/{{previousTag}}...{{currentTag}}",
        "https://www.gitlab.com/myOrg/myRepo/compare/v1.2.2...v1.2.3")]
    public void ShouldBuildVersionTagLink(string compareUrlFormat, string expected)
    {
        SemanticVersion newVersion = SemanticVersion.Parse("1.2.3");
        SemanticVersion previousVersion = SemanticVersion.Parse("1.2.2");

        var linkBuilder = new GitlabLinkBuilder("https://gitlab.com/myOrg/myRepo.git");
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
