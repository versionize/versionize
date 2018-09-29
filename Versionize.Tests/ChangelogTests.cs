using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using System.Collections.Generic;

namespace Versionize.Tests
{
    public class ChangelogTests : IDisposable
    {
        private string _testDirectory;

        public ChangelogTests()
        {
            // TODO: problems with parallel tests
            var testDirectory = Path.Combine(Path.GetTempPath(), "ChangelogTests");
            Directory.CreateDirectory(testDirectory);

            _testDirectory = testDirectory;
        }

        [Fact]
        public void ShouldGenerateAChangelogEvenForEmptyCommits()
        {
            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1,1,0), new DateTimeOffset(), new List<ConventionalCommit>());

            var wasChangelogWritten = File.Exists(Path.Join(_testDirectory, "CHANGELOG.md"));
            Assert.True(wasChangelogWritten);
        }

        [Fact]
        public void ShouldGenerateAChangelogForFixFeatAndBreakingCommits()
        {
            ConventionalCommitParser parser = new ConventionalCommitParser();

            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1,1,0), new DateTimeOffset(), new List<ConventionalCommit> {
                parser.Parse(new TestCommit("fix: a fix")),
                parser.Parse(new TestCommit("feat: a feature")),
                parser.Parse(new TestCommit("feat: a breaking change feature\nBREAKING CHANGE: this will break everything")),
            });

            var wasChangelogWritten = File.Exists(Path.Join(_testDirectory, "CHANGELOG.md"));
            Assert.True(wasChangelogWritten);

            // TODO: Assert changelog entries
        }

        [Fact]
        public void ShouldAppendToExistingChangelog()
        {
            File.WriteAllText(Path.Combine(_testDirectory, "CHANGELOG.md"), "# Some preamble\n\n##SomeVersion");

            ConventionalCommitParser parser = new ConventionalCommitParser();

            var changelog = Changelog.Discover(_testDirectory);
            changelog.Write(new Version(1,1,0), new DateTimeOffset(), new List<ConventionalCommit> {
                parser.Parse(new TestCommit("fix: a fix")),
                parser.Parse(new TestCommit("feat: a feature")),
                parser.Parse(new TestCommit("feat: a breaking change feature\nBREAKING CHANGE: this will break everything")),
            });

            var wasChangelogWritten = File.Exists(Path.Join(_testDirectory, "CHANGELOG.md"));
            Assert.True(wasChangelogWritten);

            // TODO: Assert changelog entries
        }

        [Fact]
        public void ShouldExposeFilePathProperty()
        {
            var changelog = Changelog.Discover(_testDirectory);
         
            Assert.Equal(Path.Combine(_testDirectory, "CHANGELOG.md"), changelog.FilePath);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch (Exception) { }
        }
    }
}
