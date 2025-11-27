using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public class ChangelogCommandTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public ChangelogCommandTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void OnlyIncludesChangesForSpecifiedVersion()
    {
        var fileCommitter = new FileCommitter(_testSetup);

        // 1.0.0
        fileCommitter.CommitChange("feat: commit 1");
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only"]);
        exitCode.ShouldBe(0);

        // 1.1.0
        fileCommitter.CommitChange("feat: commit 2");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only"]);
        exitCode.ShouldBe(0);

        // 1.2.0
        fileCommitter.CommitChange("feat: commit 3");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only"]);
        exitCode.ShouldBe(0);

        // extra commit
        fileCommitter.CommitChange("feat: commit 4");
        _testPlatformAbstractions.Messages.Clear();

        var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
        tags.Count.ShouldBe(3);
        tags.ShouldContain("v1.0.0");
        tags.ShouldContain("v1.1.0");
        tags.ShouldContain("v1.2.0");

        // Act
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "changelog", "--version 1.1.0", "--preamble # What's Changed?\n\n"]);
        exitCode.ShouldBe(0);

        // Assert
        _testPlatformAbstractions.Messages.Count.ShouldBe(1);
        _testPlatformAbstractions.Messages[0].ShouldContain("# What's Changed?\n\n### Features\n\n* commit 2");
    }

    [Fact]
    public void IncludesChangesSinceLastFullRelease_When_AggregatePrereleasesIsTrue()
    {
        var fileCommitter = new FileCommitter(_testSetup);
        var versionizeOptions = new VersionizeOptions
        {
            SkipBumpFile = true,
            AggregatePrereleases = true,
        };
        var prereleaseOptions = new VersionizeOptions
        {
            SkipBumpFile = true,
            AggregatePrereleases = true,
            Prerelease = "alpha",
        };

        // 1.0.0
        fileCommitter.CommitChange("feat: commit 1");
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // 1.1.0-alpha.0
        fileCommitter.CommitChange("feat: commit 2");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--pre-release alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // 1.1.0
        fileCommitter.CommitChange("feat: commit 3");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // extra commit
        fileCommitter.CommitChange("feat: commit 4");
        _testPlatformAbstractions.Messages.Clear();

        var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
        tags.Count.ShouldBe(3);
        tags.ShouldContain("v1.0.0");
        tags.ShouldContain("v1.1.0-alpha.0");
        tags.ShouldContain("v1.1.0");

        // Act
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--aggregate-pre-releases", "changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testPlatformAbstractions.Messages.Count.ShouldBe(1);
        _testPlatformAbstractions.Messages[0].ShouldBeEquivalentTo("### Features\n\n* commit 2\n* commit 3");
    }

    [Fact]
    public void IncludesChangesSinceLastRelease()
    {
        var fileCommitter = new FileCommitter(_testSetup);

        // 1.0.0
        fileCommitter.CommitChange("feat: commit 1");
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only"]);
        exitCode.ShouldBe(0);

        // 1.1.0-alpha.0
        fileCommitter.CommitChange("feat: commit 2");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--pre-release alpha"]);
        exitCode.ShouldBe(0);

        // 1.1.0
        fileCommitter.CommitChange("feat: commit 3");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only"]);
        exitCode.ShouldBe(0);

        // extra commit
        fileCommitter.CommitChange("feat: commit 4");
        _testPlatformAbstractions.Messages.Clear();

        var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
        tags.Count.ShouldBe(3);
        tags.ShouldContain("v1.0.0");
        tags.ShouldContain("v1.1.0-alpha.0");
        tags.ShouldContain("v1.1.0");

        // Act
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testPlatformAbstractions.Messages.Count.ShouldBe(1);
        _testPlatformAbstractions.Messages[0].ShouldBeEquivalentTo("### Features\n\n* commit 3");
    }

    [Fact]
    public void GeneratesChangelogForPrerelease_When_AggregatePrereleases()
    {
        var fileCommitter = new FileCommitter(_testSetup);

        // 1.0.0
        fileCommitter.CommitChange("feat: commit 1");
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // 1.1.0-alpha.0
        fileCommitter.CommitChange("feat: commit 2");
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only", "--pre-release alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        _testPlatformAbstractions.Messages.Clear();

        var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
        tags.Count.ShouldBe(2);
        tags.ShouldContain("v1.0.0");
        tags.ShouldContain("v1.1.0-alpha.0");

        // Act - Generate changelog for the prerelease version
        exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--tag-only",
            "--aggregate-pre-releases", "changelog", "-v 1.1.0-alpha.0"]);
        exitCode.ShouldBe(0);

        // Assert - Should include changes since last full release (1.0.0), not return entire history
        _testPlatformAbstractions.Messages.Count.ShouldBe(1);
        _testPlatformAbstractions.Messages[0].ShouldBeEquivalentTo("### Features\n\n* commit 2");
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
