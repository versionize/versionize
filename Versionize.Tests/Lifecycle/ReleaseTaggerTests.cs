using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.Git;
using Versionize.Config;

namespace Versionize.Lifecycle;

public class ReleaseTaggerTests : IDisposable
{
    private readonly TestSetup _testSetup;

    public ReleaseTaggerTests()
    {
        _testSetup = TestSetup.Create();
        CommandLineUI.Platform = new TestPlatformAbstractions();
    }

    [Fact]
    public void DoesntCreateATag_When_DryRunIsTrueAndSkipTagIsFalse()
    {
        // Arrange
        var options = new ReleaseTagger.Options
        {
            DryRun = true,
            SkipTag = false,
            Sign = false,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        // Act
        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        // Assert
        _testSetup.Repository.Tags.Count().ShouldBe(0);
    }

    [Fact]
    public void DoesntCreateATag_When_DryRunIsFalseAndSkipTagIsTrue()
    {
        // Arrange
        var options = new ReleaseTagger.Options
        {
            DryRun = false,
            SkipTag = true,
            Sign = false,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        // Act
        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        // Assert
        _testSetup.Repository.Tags.Count().ShouldBe(0);
    }

    [Fact]
    public void CreatesATag_When_DryRunIsFalseAndSkipTagIsFalse()
    {
        // Arrange
        var options = new ReleaseTagger.Options
        {
            DryRun = false,
            SkipTag = false,
            Sign = false,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var fileCommitter = new FileCommitter(_testSetup);
        fileCommitter.CommitChange("feat: initial commit");

        _testSetup.Repository.Commits.Count().ShouldBe(1);

        // Act
        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        // Assert
        _testSetup.Repository.Tags.Count().ShouldBe(1);
        var tag = _testSetup.Repository.Tags.Single();
        tag.FriendlyName.ShouldBe("v1.2.3");
        GitProcessUtil.IsTagSigned(_testSetup.WorkingDirectory, tag).ShouldBeFalse();
    }

    [Fact]
    public void CreatesASignedTag_When_DryRunIsFalseAndSkipTagIsFalseAndSignIsTrue()
    {
        // Arrange
        var gpgFilePath = "./TestKeyForGpgSigning.pgp";
        GitProcessUtil.RunGpgCommand($"--import \"{gpgFilePath}\"");
        _testSetup.Repository.Config.Set("user.signingkey", "0C79B0FDFF00BDF6");

        var options = new ReleaseTagger.Options
        {
            DryRun = false,
            SkipTag = false,
            Sign = true,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var fileCommitter = new FileCommitter(_testSetup);
        fileCommitter.CommitChange("feat: initial commit");

        _testSetup.Repository.Commits.Count().ShouldBe(1);

        // Act
        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        // Assert
        _testSetup.Repository.Tags.Count().ShouldBe(1);
        var tag = _testSetup.Repository.Tags.Single();
        tag.FriendlyName.ShouldBe("v1.2.3");
        GitProcessUtil.IsTagSigned(_testSetup.WorkingDirectory, tag).ShouldBeTrue();
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
