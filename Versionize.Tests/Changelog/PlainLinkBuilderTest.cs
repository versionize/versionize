using LibGit2Sharp;
using Shouldly;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog;

public class PlainLinkBuilderTest
{
    [Fact]
    public void ShouldCreatePlainLinkBuilder()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        linkBuilder.ShouldBeAssignableTo<PlainLinkBuilder>();

        linkBuilder.BuildIssueLink("123")
            .ShouldBeEmpty();
        linkBuilder.BuildCommitLink(
                new ConventionalCommit
                {
                    Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
                })
            .ShouldBeEmpty();
        linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2")
            .ShouldBeEmpty();
    }

    [Fact]
    public void ShouldBuildCustomLinks()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(
            repo,
            new ChangelogLinkTemplates
            {
                IssueLink = "https://my-repo/issues/{issue}",
                CommitLink = "https://my-repo/commits/{commitSha}",
                VersionTagLink = "https://my-repo/tags/v{version}",
            });

        linkBuilder.ShouldBeAssignableTo<TemplatedLinkBuilder>();

        linkBuilder.BuildIssueLink("123")
            .ShouldBe("https://my-repo/issues/123");
        linkBuilder.BuildCommitLink(
                new ConventionalCommit
                {
                    Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
                })
            .ShouldBe("https://my-repo/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2")
            .ShouldBe("https://my-repo/tags/v1.2.3");
    }

    [Fact]
    public void CustomTemplatesShouldHavePriorityOverDefaultProviders()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://github.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(
            repo,
            new ChangelogLinkTemplates
            {
                IssueLink = "https://my-repo/issues/{issue}",
                CommitLink = "https://my-repo/commits/{commitSha}",
                VersionTagLink = "https://my-repo/tags/v{version}",
            });

        linkBuilder.ShouldBeAssignableTo<TemplatedLinkBuilder>();

        linkBuilder.BuildIssueLink("123")
            .ShouldBe("https://my-repo/issues/123");
        linkBuilder.BuildCommitLink(
                new ConventionalCommit
                {
                    Sha = "734713bc047d87bf7eac9674765ae793478c50d3"
                })
            .ShouldBe("https://my-repo/commits/734713bc047d87bf7eac9674765ae793478c50d3");
        linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2")
            .ShouldBe("https://my-repo/tags/v1.2.3");
    }

    [Fact]
    public void ShouldBuildVersionTagLinkThatUsesPreviousTag()
    {
        var repo = SetupRepositoryWithRemote("origin", "https://github.com/versionize/versionize.git");
        var linkBuilder = LinkBuilderFactory.CreateFor(
            repo,
            new ChangelogLinkTemplates
            {
                VersionTagLink = "https://github.com/versionize/versionize/compare/{previousTag}...{currentTag}",
            });

        linkBuilder.ShouldBeAssignableTo<TemplatedLinkBuilder>();

        linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2")
            .ShouldBe("https://github.com/versionize/versionize/compare/v1.2.2...v1.2.3");
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
