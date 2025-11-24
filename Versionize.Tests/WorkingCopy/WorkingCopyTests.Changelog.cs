using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public partial class WorkingCopyTests
{
    public class ChangelogCmd : IDisposable
    {
        private readonly TestSetup _testSetup;
        private readonly TestPlatformAbstractions _cliAbstraction;

        public ChangelogCmd()
        {
            _testSetup = TestSetup.Create();
            CommandLineUI.Platform = _cliAbstraction = new TestPlatformAbstractions();
        }

        [Theory]
        [InlineData(null, "### Features\n\n* commit 3")]
        [InlineData("1.2.0", "### Features\n\n* commit 3")]
        [InlineData("1.1.0", "### Features\n\n* commit 2")]
        [InlineData("1.0.0", "### Features\n\n* commit 1")]
        public void OnlyIncludesChangesForSpecifiedVersion(string versionStr, string expectedChangelog)
        {
            var fileCommitter = new FileCommitter(_testSetup);
            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            var versionizeOptions = new VersionizeOptions
            {
                SkipBumpFile = true,
            };

            // 1.0.0
            fileCommitter.CommitChange("feat: commit 1");
            workingCopy.Versionize(versionizeOptions);

            // 1.1.0
            fileCommitter.CommitChange("feat: commit 2");
            workingCopy.Versionize(versionizeOptions);

            // 1.2.0
            fileCommitter.CommitChange("feat: commit 3");
            workingCopy.Versionize(versionizeOptions);

            // extra commit
            fileCommitter.CommitChange("feat: commit 4");
            _cliAbstraction.Messages.Clear();

            var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
            tags.Count.ShouldBe(3);
            tags.ShouldContain("v1.0.0");
            tags.ShouldContain("v1.1.0");
            tags.ShouldContain("v1.2.0");

            // Act
            workingCopy.GenerateChangelog(versionizeOptions, versionStr, null);

            // Assert
            _cliAbstraction.Messages.Count.ShouldBe(1);
            _cliAbstraction.Messages[0].ShouldBeEquivalentTo(expectedChangelog);
        }

        [Fact]
        public void IncludesChangesSinceLastFullRelease_When_AggregatePrereleasesIsTrue()
        {
            var fileCommitter = new FileCommitter(_testSetup);
            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
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
            workingCopy.Versionize(versionizeOptions);

            // 1.1.0-alpha.0
            fileCommitter.CommitChange("feat: commit 2");
            workingCopy.Versionize(prereleaseOptions);

            // 1.1.0
            fileCommitter.CommitChange("feat: commit 3");
            workingCopy.Versionize(versionizeOptions);

            // extra commit
            fileCommitter.CommitChange("feat: commit 4");
            _cliAbstraction.Messages.Clear();

            var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
            tags.Count.ShouldBe(3);
            tags.ShouldContain("v1.0.0");
            tags.ShouldContain("v1.1.0-alpha.0");
            tags.ShouldContain("v1.1.0");

            // Act
            workingCopy.GenerateChangelog(versionizeOptions, null, null);

            // Assert
            _cliAbstraction.Messages.Count.ShouldBe(1);
            _cliAbstraction.Messages[0].ShouldBeEquivalentTo("### Features\n\n* commit 2\n* commit 3");
        }

        [Fact]
        public void IncludesChangesSinceLastRelease()
        {
            var fileCommitter = new FileCommitter(_testSetup);
            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            var versionizeOptions = new VersionizeOptions
            {
                SkipBumpFile = true,
            };
            var prereleaseOptions = new VersionizeOptions
            {
                SkipBumpFile = true,
                Prerelease = "alpha",
            };

            // 1.0.0
            fileCommitter.CommitChange("feat: commit 1");
            workingCopy.Versionize(versionizeOptions);

            // 1.1.0-alpha.0
            fileCommitter.CommitChange("feat: commit 2");
            workingCopy.Versionize(prereleaseOptions);

            // 1.1.0
            fileCommitter.CommitChange("feat: commit 3");
            workingCopy.Versionize(versionizeOptions);

            // extra commit
            fileCommitter.CommitChange("feat: commit 4");
            _cliAbstraction.Messages.Clear();

            var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
            tags.Count.ShouldBe(3);
            tags.ShouldContain("v1.0.0");
            tags.ShouldContain("v1.1.0-alpha.0");
            tags.ShouldContain("v1.1.0");

            // Act
            workingCopy.GenerateChangelog(versionizeOptions, null, null);

            // Assert
            _cliAbstraction.Messages.Count.ShouldBe(1);
            _cliAbstraction.Messages[0].ShouldBeEquivalentTo("### Features\n\n* commit 3");
        }

        [Fact]
        public void GeneratesChangelogForPrerelease_When_AggregatePrereleases()
        {
            var fileCommitter = new FileCommitter(_testSetup);
            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
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
            workingCopy.Versionize(versionizeOptions);

            // 1.1.0-alpha.0
            fileCommitter.CommitChange("feat: commit 2");
            workingCopy.Versionize(prereleaseOptions);

            _cliAbstraction.Messages.Clear();

            var tags = _testSetup.Repository.Tags.Select(x => x.FriendlyName).ToList();
            tags.Count.ShouldBe(2);
            tags.ShouldContain("v1.0.0");
            tags.ShouldContain("v1.1.0-alpha.0");

            // Act - Generate changelog for the prerelease version
            workingCopy.GenerateChangelog(prereleaseOptions, "1.1.0-alpha.0", null);

            // Assert - Should include changes since last full release (1.0.0), not return entire history
            _cliAbstraction.Messages.Count.ShouldBe(1);
            _cliAbstraction.Messages[0].ShouldBeEquivalentTo("### Features\n\n* commit 2");
        }

        public void Dispose()
        {
            _testSetup.Dispose();
        }
    }
}
