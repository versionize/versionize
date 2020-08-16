using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using System.Collections.Generic;
using Shouldly;

namespace Versionize.Tests
{
    public class ChangelogTests : IDisposable
    {
        private readonly string _testDirectory;

        public ChangelogTests()
        {
            _testDirectory = TempDir.Create();
        }

        [Fact]
        public void ShouldGenerateAChangelogEvenForEmptyCommits()
        {
            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1, 1, 0), new DateTimeOffset(), new List<ConventionalCommit>());

            var wasChangelogWritten = File.Exists(Path.Join(_testDirectory, "CHANGELOG.md"));
            Assert.True(wasChangelogWritten);
        }

        [Fact]
        public void ShouldGenerateAChangelogForFixFeatAndBreakingCommits()
        {
            var parser = new ConventionalCommitParser();

            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1, 1, 0), new DateTimeOffset(), new List<ConventionalCommit>
            {
                parser.Parse(new TestCommit("fix: a fix")),
                parser.Parse(new TestCommit("feat: a feature")),
                parser.Parse(
                    new TestCommit("feat: a breaking change feature\nBREAKING CHANGE: this will break everything")),
            });

            var wasChangelogWritten = File.Exists(Path.Join(_testDirectory, "CHANGELOG.md"));
            Assert.True(wasChangelogWritten);

            // TODO: Assert changelog entries
        }

        [Fact]
        public void ShouldAppendAtEndIfChangelogContainsExtraInformation()
        {
            File.WriteAllText(Path.Combine(_testDirectory, "CHANGELOG.md"), "# Should be kept by versionize\n\nSome information about the changelog");
            
            var parser = new ConventionalCommitParser();
            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1, 0, 0), new DateTimeOffset(), new List<ConventionalCommit>
            {
                parser.Parse(new TestCommit("fix: a fix in version 1.0.0")),
            });
            
            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldBe("# Should be kept by versionize\n\nSome information about the changelog\n\n<a name=\"1.0.0\"></a>\n## 1.0.0 (1-1-1)\n\n### Bug Fixes\n\n* a fix in version 1.0.0\n\n");
        }

        [Fact]
        public void ShouldAppendToExistingChangelog()
        {
            var parser = new ConventionalCommitParser();

            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1, 0, 0), new DateTimeOffset(), new List<ConventionalCommit>
            {
                parser.Parse(new TestCommit("fix: a fix in version 1.0.0")),
            });
            
            changelog.Write(new Version(1, 1, 0), new DateTimeOffset(), new List<ConventionalCommit>
            {
                parser.Parse(new TestCommit("fix: a fix in version 1.1.0")),
            });

            var changelogContents = File.ReadAllText(changelog.FilePath);
            
            changelogContents.ShouldContain("<a name=\"1.0.0\"></a>");
            changelogContents.ShouldContain("a fix in version 1.0.0");
            
            changelogContents.ShouldContain("<a name=\"1.1.0\"></a>");
            changelogContents.ShouldContain("a fix in version 1.1.0");
        }

        [Fact]
        public void ShouldExposeFilePathProperty()
        {
            var changelog = Changelog.Discover(_testDirectory);

            Assert.Equal(Path.Combine(_testDirectory, "CHANGELOG.md"), changelog.FilePath);
        }

        [Fact]
        public void ShouldIncludeAllCommitsInChangelogWhenGiven()
        {
            var parser = new ConventionalCommitParser();

            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1, 1, 0), new DateTimeOffset(), new List<ConventionalCommit>
            {
                parser.Parse(new TestCommit("chore: nothing important")),
                parser.Parse(new TestCommit("chore: some foo bar")),
            }, true);

            var changelogContents = File.ReadAllText(changelog.FilePath);

            changelogContents.ShouldContain("nothing important");
            changelogContents.ShouldContain("some foo bar");
        }
        
        public void Dispose()
        {
            Cleanup.DeleteDirectory(_testDirectory);
        }
    }
}
