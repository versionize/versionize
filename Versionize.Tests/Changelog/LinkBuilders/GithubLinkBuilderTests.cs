using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
using Versionize.ConventionalCommits;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog.LinkBuilders;

public class GithubLinkBuilderTests : IDisposable
{
    private Repository _repo;

    [Fact]
    public void ShouldThrowIfUrlIsNoRecognizedSshOrHttpsUrl()
    {
        Should.Throw<VersionizeException>(() => new GithubLinkBuilder("github.com"));
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForHTTPSPushUrls()
    {
        _repo = SetupRepositoryWithRemote("origin", "https://github.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForSSHPushUrls()
    {
        _repo = SetupRepositoryWithRemote("origin", "git@github.com:versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldPickFirstRemoteInCaseNoOriginWasFound()
    {
        _repo = SetupRepositoryWithRemote("some", "git@github.com:versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldFallbackToNoopInCaseNoGithubPushUrlWasDefined()
    {
        _repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<NullLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForSSHPushUrlsEvenWithoutGitSuffix()
    {
        _repo = SetupRepositoryWithRemote("origin", "git@github.com:versionize/versionize");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

        linkBuilder.ShouldBeAssignableTo<GithubLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateAGithubUrlBuilderForHTTPSPushUrlsEvenWithoutGitSuffix()
    {
        _repo = SetupRepositoryWithRemote("origin", "https://github.com/versionize/versionize");
        var linkBuilder = LinkBuilderFactory.CreateFor(_repo);

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
