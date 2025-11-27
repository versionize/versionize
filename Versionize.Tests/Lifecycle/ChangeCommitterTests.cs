using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.BumpFiles;
using Versionize.Changelog;
using Versionize.Git;
using Versionize.Config;
using Versionize.Changelog.LinkBuilders;

namespace Versionize.Lifecycle;

public class ChangeCommitterTests : IDisposable
{
    private readonly TestSetup _testSetup;

    public ChangeCommitterTests()
    {
        _testSetup = TestSetup.Create();
        CommandLineUI.Platform = new TestPlatformAbstractions();
    }

    [Fact]
    public void DoesntCreateACommit_When_DryRunIsTrueAndSkipCommitIsFalse()
    {
        // Arrange
        var options = new ChangeCommitter.Options
        {
            DryRun = true,
            Sign = false,
            CommitSuffix = "",
            SkipCommit = false,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var bumpFile = NullBumpFile.Default;
        ChangelogBuilder changelog = null;

        // Act
        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(0);
    }

    [Fact]
    public void DoesntCreateACommit_When_DryRunIsFalseAndSkipCommitIsTrue()
    {
        // Arrange
        var options = new ChangeCommitter.Options
        {
            DryRun = false,
            Sign = false,
            CommitSuffix = "",
            SkipCommit = true,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var bumpFile = NullBumpFile.Default;
        ChangelogBuilder changelog = null;

        // Act
        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(0);
    }

    [Theory]
    [InlineData("", "chore(release): 2.0.0")]
    [InlineData(null, "chore(release): 2.0.0")]
    [InlineData("[skip ci]", "chore(release): 2.0.0 [skip ci]")]
    public void CreatesACommit_When_DryRunIsFalseAndSkipCommitIsFalse(string commitSuffix, string expectedMessage)
    {
        // Arrange
        var options = new ChangeCommitter.Options
        {
            DryRun = false,
            Sign = false,
            CommitSuffix = commitSuffix,
            SkipCommit = false,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var bumpFile = NullBumpFile.Default;

        ChangelogBuilder changelog = ChangelogBuilder.CreateForPath(_testSetup.WorkingDirectory);
        changelog.Write(
            Version.Parse("2.0.0"),
            Version.Parse("2.0.0"),
            DateTimeOffset.Now,
            new NullLinkBuilder(),
            [],
            ProjectOptions.DefaultOneProjectPerRepo);

        // Act
        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(1);
        var commit = _testSetup.Repository.Commits.First();
        var actualMessage = commit.Message.TrimEnd();
        actualMessage.ShouldBe(expectedMessage);
        GitProcessUtil.IsCommitSigned(_testSetup.WorkingDirectory, commit).ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "chore(release): 2.0.0")]
    [InlineData(null, "chore(release): 2.0.0")]
    [InlineData("[skip ci]", "chore(release): 2.0.0 [skip ci]")]
    public void CreatesASignedCommit_When_DryRunIsFalseAndSkipCommitIsFalseAndSignIsTrue(string commitSuffix, string expectedMessage)
    {
        // Arrange
        var gpgFilePath = "./TestData/TestKeyForGpgSigning.pgp";
        GitProcessUtil.RunGpgCommand($"--import \"{gpgFilePath}\"");
        _testSetup.Repository.Config.Set("user.signingkey", "0C79B0FDFF00BDF6");

        var options = new ChangeCommitter.Options
        {
            DryRun = false,
            Sign = true,
            CommitSuffix = commitSuffix,
            SkipCommit = false,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var bumpFile = NullBumpFile.Default;

        ChangelogBuilder changelog = ChangelogBuilder.CreateForPath(_testSetup.WorkingDirectory);
        changelog.Write(
            Version.Parse("2.0.0"),
            Version.Parse("2.0.0"),
            DateTimeOffset.Now,
            new NullLinkBuilder(),
            [],
            ProjectOptions.DefaultOneProjectPerRepo);

        // Act
        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(1);
        var commit = _testSetup.Repository.Commits.First();
        var actualMessage = commit.Message.TrimEnd();
        actualMessage.ShouldBe(expectedMessage);
        GitProcessUtil.IsCommitSigned(_testSetup.WorkingDirectory, commit).ShouldBeTrue();
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
