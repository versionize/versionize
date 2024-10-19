using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.BumpFiles;
using Versionize.Changelog;
using Versionize.Git;

namespace Versionize.Tests;

public class ChangeCommitterTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public ChangeCommitterTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void DoesntCreateACommit_When_DryRunIsTrueAndSkipCommitIsFalse()
    {
        var options = new ChangeCommitter.Options
        {
            DryRun = true,
            Sign = false,
            CommitSuffix = "",
            SkipCommit = false,
        };
        var bumpFile = new NullBumpFile();
        ChangelogBuilder changelog = null;

        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

        _testSetup.Repository.Commits.Count().ShouldBe(0);
    }

    [Fact]
    public void DoesntCreateACommit_When_DryRunIsFalseAndSkipCommitIsTrue()
    {
        var options = new ChangeCommitter.Options
        {
            DryRun = false,
            Sign = false,
            CommitSuffix = "",
            SkipCommit = true,
        };
        var bumpFile = new NullBumpFile();
        ChangelogBuilder changelog = null;

        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

        _testSetup.Repository.Commits.Count().ShouldBe(0);
    }

    [Theory]
    [InlineData("", "chore(release): 2.0.0")]
    [InlineData(null, "chore(release): 2.0.0")]
    [InlineData("[skip ci]", "chore(release): 2.0.0 [skip ci]")]
    public void CreatesACommit_When_DryRunIsFalseAndSkipCommitIsFalse(string commitSuffix, string expectedMessage)
    {
        var options = new ChangeCommitter.Options
        {
            DryRun = false,
            Sign = false,
            CommitSuffix = commitSuffix,
            SkipCommit = false,
        };
        var bumpFile = new NullBumpFile();

        var changelogOptions = Config.ChangelogOptions.Default;
        ChangelogBuilder changelog = ChangelogBuilder.CreateForPath(_testSetup.WorkingDirectory);
        changelog.Write(
            Version.Parse("2.0.0"),
            DateTimeOffset.Now,
            new PlainLinkBuilder(),
            [],
            changelogOptions);

        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

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
        var gpgFilePath = "./TestKeyForGpgSigning.pgp";
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
        var bumpFile = new NullBumpFile();

        var changelogOptions = Config.ChangelogOptions.Default;
        ChangelogBuilder changelog = ChangelogBuilder.CreateForPath(_testSetup.WorkingDirectory);
        changelog.Write(
            Version.Parse("2.0.0"),
            DateTimeOffset.Now,
            new PlainLinkBuilder(),
            [],
            changelogOptions);

        ChangeCommitter.CreateCommit(
            _testSetup.Repository,
            options,
            new Version(2, 0, 0),
            bumpFile,
            changelog);

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
