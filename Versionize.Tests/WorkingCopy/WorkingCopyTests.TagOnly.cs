using Shouldly;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public partial class WorkingCopyTests
{
    private readonly VersionizeOptions _defaultTagOnlyOptions = new()
    {
        SkipDirty = true,
        BumpFileType = BumpFileType.None,
        SkipChangelog = true,
    };

    [Fact]
    public void ShouldTagInitialVersionUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

        // Act
        workingCopy.Versionize(_defaultTagOnlyOptions);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(1);
        var commitThatShouldBeTagged = _testSetup.Repository.Commits.First();

        _testSetup.Repository.Tags.Count().ShouldBe(1);
        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(["v1.0.0"]);
        var tag = _testSetup.Repository.Tags.First();
        tag.Annotation.Target.Sha.ShouldBe(commitThatShouldBeTagged.Sha);
    }

    [Fact]
    public void ShouldTagInitialVersionUsingTagOnlyWithNonTrackedCommits()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline");
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline2");
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline3");
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline4");

        // Act
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(_defaultTagOnlyOptions);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(5);
        _testSetup.Repository.Tags.Count().ShouldBe(1);
        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(["v1.0.0"]);
    }

    [Fact]
    public void ShouldTagVersionAfterFeatUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(_defaultTagOnlyOptions);

        new FileCommitter(_testSetup).CommitChange("feat: first feature");

        // Act
        workingCopy.Versionize(_defaultTagOnlyOptions);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.1.0"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(2);
    }

    [Fact]
    public void ShouldTagVersionAfterFixUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(_defaultTagOnlyOptions);

        new FileCommitter(_testSetup).CommitChange("fix: first feature");

        // Act
        workingCopy.Versionize(_defaultTagOnlyOptions);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.0.1"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(2);
    }

    [Fact]
    public void ShouldTagVersionWhenMultipleCommitsInOneVersionUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(_defaultTagOnlyOptions);

        var fc = new FileCommitter(_testSetup);
        fc.CommitChange("fix: first fix");
        fc.CommitChange("fix: second fix");
        fc.CommitChange("feat: first feature");

        // Act
        workingCopy.Versionize(_defaultTagOnlyOptions);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.1.0"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(4);
    }

    [Fact]
    public void ShouldTagVersionAfterEachVersionizeCommandUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(_defaultTagOnlyOptions);

        new FileCommitter(_testSetup).CommitChange("fix: first fix");
        workingCopy.Versionize(_defaultTagOnlyOptions);
        new FileCommitter(_testSetup).CommitChange("fix: another fix");

        // Act
        workingCopy.Versionize(_defaultTagOnlyOptions);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.0.1", "v1.0.2"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(3);
    }
}
