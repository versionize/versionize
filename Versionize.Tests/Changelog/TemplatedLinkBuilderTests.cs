using Shouldly;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Xunit;

namespace Versionize.Changelog;

public class TemplatedLinkBuilderTests
{
    [Fact]
    public void ShouldBuildCustomLinks()
    {
        var fallbackLinkBuilder = new PlainLinkBuilder();
        var linkBuilder = new TemplatedLinkBuilder(
            new ChangelogLinkTemplates
            {
                IssueLink = "https://my-repo/issues/{issue}",
                CommitLink = "https://my-repo/commits/{commitSha}",
                VersionTagLink = "https://my-repo/tags/v{version}",
            },
            fallbackLinkBuilder);

        var issueLink = linkBuilder.BuildIssueLink("123");
        issueLink.ShouldBe("https://my-repo/issues/123");

        var commit = new ConventionalCommit { Sha = "734713bc047d87bf7eac9674765ae793478c50d3" };
        var commitLink = linkBuilder.BuildCommitLink(commit);
        commitLink.ShouldBe("https://my-repo/commits/734713bc047d87bf7eac9674765ae793478c50d3");

        var versionTagLink = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");
        versionTagLink.ShouldBe("https://my-repo/tags/v1.2.3");
    }

    [Fact]
    public void CustomTemplatesShouldHavePriorityOverDefaultProviders()
    {
        var fallbackLinkBuilder = new GithubLinkBuilder("https://github.com/versionize/versionize.git");
        var linkBuilder = new TemplatedLinkBuilder(
            new ChangelogLinkTemplates
            {
                IssueLink = "https://my-repo/issues/{issue}",
                CommitLink = "https://my-repo/commits/{commitSha}",
                VersionTagLink = "https://my-repo/tags/v{version}",
            },
            fallbackLinkBuilder);

        var issueLink = linkBuilder.BuildIssueLink("123");
        issueLink.ShouldBe("https://my-repo/issues/123");

        var commit = new ConventionalCommit { Sha = "734713bc047d87bf7eac9674765ae793478c50d3" };
        var commitLink = linkBuilder.BuildCommitLink(commit);
        commitLink.ShouldBe("https://my-repo/commits/734713bc047d87bf7eac9674765ae793478c50d3");

        var versionTagLink = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");
        versionTagLink.ShouldBe("https://my-repo/tags/v1.2.3");
    }

    [Fact]
    public void ShouldBuildVersionTagLinkThatUsesPreviousTag()
    {
        var fallbackLinkBuilder = new PlainLinkBuilder();
        var linkBuilder = new TemplatedLinkBuilder(
            new ChangelogLinkTemplates
            {
                VersionTagLink = "https://github.com/versionize/versionize/compare/{previousTag}...{currentTag}",
            },
            fallbackLinkBuilder);

        var versionTagLink = linkBuilder.BuildVersionTagLink("v1.2.3", "v1.2.2");
        versionTagLink.ShouldBe("https://github.com/versionize/versionize/compare/v1.2.2...v1.2.3");
    }
}
