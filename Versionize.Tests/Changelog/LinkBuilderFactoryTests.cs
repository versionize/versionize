using LibGit2Sharp;
using Shouldly;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Changelog;

public class LinkBuilderFactoryTests
{
    [Fact]
    public void ShouldCreateNullLinkBuilder()
    {
        // Arrange
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");

        // Act
        var linkBuilder = LinkBuilderFactory.CreateFor(repo);

        // Assert
        linkBuilder.ShouldBeAssignableTo<NullLinkBuilder>();
    }

    [Fact]
    public void ShouldCreateTemplatedLinkBuilder()
    {
        // Arrange
        var repo = SetupRepositoryWithRemote("origin", "https://hostmeister.com/versionize/versionize.git");

        // Act
        var linkBuilder = LinkBuilderFactory.CreateFor(
            repo,
            new ChangelogLinkTemplates
            {
                IssueLink = "https://my-repo/issues/{issue}",
                CommitLink = "https://my-repo/commits/{commitSha}",
                VersionTagLink = "https://my-repo/tags/v{version}",
            });

        // Assert
        linkBuilder.ShouldBeAssignableTo<TemplatedLinkBuilder>();
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
