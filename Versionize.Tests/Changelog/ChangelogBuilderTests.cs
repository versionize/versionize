using Xunit;
using Versionize.Tests.TestSupport;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.Tests;

namespace Versionize.Changelog.Tests
{
    public class ChangelogBuilderTests : IDisposable
    {
        private readonly string _testDirectory;

        public ChangelogBuilderTests()
        {
            _testDirectory = TempDir.Create();
        }

        [Fact]
        public void ShouldGenerateAChangelogEvenForEmptyCommits()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>(),
                ChangelogOptions.Default);

            var wasChangelogWritten = File.Exists(Path.Join(_testDirectory, "CHANGELOG.md"));
            Assert.True(wasChangelogWritten);
        }

        [Fact]
        public void ShouldGenerateWithoutLiteralLineBreakCharacters()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                },
                ChangelogOptions.Default);

            var contents = File.ReadAllText(Path.Join(_testDirectory, "CHANGELOG.md"));
            contents.ShouldNotContain("\\n");
        }

        [Fact]
        public void ShouldGenerateAChangelogForFixFeatAndBreakingCommits()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a feature")),
                    ConventionalCommitParser.Parse(
                        new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a breaking change feature\nBREAKING CHANGE: this will break everything")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);

            var sb = new ChangelogStringBuilder();
            sb.Append(ChangelogOptions.Preamble);
            sb.Append("<a name=\"1.1.0\"></a>");
            sb.Append("## 1.1.0 (1-1-1)", 2);
            sb.Append("### Features", 2);
            sb.Append("* a breaking change feature");
            sb.Append("* a feature", 2);
            sb.Append("### Bug Fixes", 2);
            sb.Append("* a fix", 2);
            sb.Append("### Breaking Changes", 2);
            sb.Append("* a breaking change feature", 2);
            Assert.Equal(sb.Build(), changelogContents);
        }

        [Fact]
        public void ShouldHideFixSectionWhenHideIsTrue()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a feature")),
                },
                ChangelogOptions.Default with
                {
                    Sections = new []
                    {
                        new ChangelogSection { Type = "feat", Hidden = false, Section = "Features" },
                        new ChangelogSection { Type = "fix", Hidden = true, Section = "Bug Fixes" },
                    }
                });

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("### Features", Case.Sensitive);
            changelogContents.ShouldNotContain("Bug Fixes");
        }

        [Fact]
        public void ShouldHideFixSectionWhenSectionIsNotSpecified()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a feature")),
                },
                ChangelogOptions.Default with
                {
                    Sections = new []
                    {
                        new ChangelogSection { Type = "feat", Hidden = false, Section = "Features" },
                    }
                });

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("### Features", Case.Sensitive);
            changelogContents.ShouldNotContain("Bug Fixes");
        }

        [Fact]
        public void ShouldShowFixSectionWhenHideIsNotSpecified()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a feature")),
                },
                ChangelogOptions.Default with
                {
                    Sections = new[]
                    {
                        new ChangelogSection { Type = "feat", Hidden = false, Section = "Features" },
                        new ChangelogSection { Type = "fix", Section = "Bug Fixes" },
                    }
                });

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("### Features", Case.Sensitive);
            changelogContents.ShouldContain("### Bug Fixes", Case.Sensitive);
        }

        [Fact]
        public void ShouldIncludeFixAndFeatCommitsInOtherSectionWhenHiddenAndShowAllIsTrue()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a feature")),
                },
                ChangelogOptions.Default with
                {
                    IncludeAllCommits = true,
                    Sections = new[]
                    {
                        new ChangelogSection { Type = "feat", Hidden = true, Section = "Features" },
                        new ChangelogSection { Type = "fix", Hidden = true, Section = "Bug Fixes" },
                    }
                });

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldNotContain("Features");
            changelogContents.ShouldNotContain("Bug Fixes");
            changelogContents.ShouldContain("### Other", Case.Sensitive);
            changelogContents.ShouldContain("a fix");
            changelogContents.ShouldContain("a feature");
        }

        [Fact]
        public void ShouldUseCustomHeaderWhenSpecified()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                },
                ChangelogOptions.Default with
                {
                    Header = "My custom changelog",
                });

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldStartWith("My custom changelog", Case.Sensitive);
        }

        [Fact]
        public void ShouldAppendAtEndIfChangelogContainsExtraInformation()
        {
            File.WriteAllText(Path.Combine(_testDirectory, "CHANGELOG.md"), "# Should be kept by versionize\n\nSome information about the changelog");

            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 0, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.0.0")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldBe("# Should be kept by versionize\n\nSome information about the changelog\n\n<a name=\"1.0.0\"></a>\n## 1.0.0 (1-1-1)\n\n### Bug Fixes\n\n* a fix in version 1.0.0\n\n");
        }

        [Fact]
        public void ShouldBuildGithubHttpsCommitLinks()
        {
            var linkBuilder = new GithubLinkBuilder("https://github.com/organization/repository.git");
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 0, 0),
                new DateTimeOffset(),
                linkBuilder, new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.0.0")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("* a fix in version 1.0.0 ([a360d6a](https://www.github.com/organization/repository/commit/a360d6a307909c6e571b29d4a329fd786c5d4543))");
        }

        [Fact]
        public void ShouldBuildGithubSSHCommitLinks()
        {
            var linkBuilder = new GithubLinkBuilder("git@github.com:organization/repository.git");
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 0, 0),
                new DateTimeOffset(),
                linkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.0.0")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("* a fix in version 1.0.0 ([a360d6a](https://www.github.com/organization/repository/commit/a360d6a307909c6e571b29d4a329fd786c5d4543))");
        }

        [Fact]
        public void ShouldBuildGithubSSHVersionTagLinks()
        {
            var linkBuilder = new GithubLinkBuilder("https://github.com/organization/repository.git");
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 0, 0),
                new DateTimeOffset(),
                linkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.0.0")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("## [1.0.0](https://www.github.com/organization/repository/releases/tag/v1.0.0)");
        }

        [Fact]
        public void ShouldBuildGithubHTTPSVersionTagLinks()
        {
            var linkBuilder = new GithubLinkBuilder("git@github.com:organization/repository.git");
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 0, 0),
                new DateTimeOffset(),
                linkBuilder, new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.0.0")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);
            changelogContents.ShouldContain("## [1.0.0](https://www.github.com/organization/repository/releases/tag/v1.0.0)");
        }

        [Fact]
        public void ShouldAppendToExistingChangelog()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 0, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.0.0")),
                },
                ChangelogOptions.Default);

            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix in version 1.1.0")),
                },
                ChangelogOptions.Default);

            var changelogContents = File.ReadAllText(changelog.FilePath);

            changelogContents.ShouldContain("<a name=\"1.0.0\"></a>");
            changelogContents.ShouldContain("a fix in version 1.0.0");

            changelogContents.ShouldContain("<a name=\"1.1.0\"></a>");
            changelogContents.ShouldContain("a fix in version 1.1.0");
        }

        [Fact]
        public void ShouldExposeFilePathProperty()
        {
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);

            Assert.Equal(Path.Combine(_testDirectory, "CHANGELOG.md"), changelog.FilePath);
        }

        [Fact]
        public void ShouldIncludeAllCommitsInChangelogWhenGiven()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            var changelog = ChangelogBuilder.CreateForPath(_testDirectory);
            changelog.Write(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "chore: nothing important")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "chore: some foo bar")),
                },
                ChangelogOptions.Default with { IncludeAllCommits = true });

            var changelogContents = File.ReadAllText(changelog.FilePath);

            changelogContents.ShouldContain("nothing important");
            changelogContents.ShouldContain("some foo bar");
        }

        [Fact]
        public void GenerateMarkdownShouldGenerateMarkdownForFixFeatAndBreakingCommits()
        {
            var plainLinkBuilder = new PlainLinkBuilder();
            string markdown = ChangelogBuilder.GenerateMarkdown(
                new Version(1, 1, 0),
                new DateTimeOffset(),
                plainLinkBuilder,
                new List<ConventionalCommit>
                {
                    ConventionalCommitParser.Parse(new TestCommit("a360d6a307909c6e571b29d4a329fd786c5d4543", "fix: a fix")),
                    ConventionalCommitParser.Parse(new TestCommit("b360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a feature")),
                    ConventionalCommitParser.Parse(
                        new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "feat: a breaking change feature\nBREAKING CHANGE: this will break everything")),
                },
                ChangelogOptions.Default);

            var sb = new ChangelogStringBuilder();
            sb.Append("<a name=\"1.1.0\"></a>");
            sb.Append("## 1.1.0 (1-1-1)", 2);
            sb.Append("### Features", 2);
            sb.Append("* a breaking change feature");
            sb.Append("* a feature", 2);
            sb.Append("### Bug Fixes", 2);
            sb.Append("* a fix", 2);
            sb.Append("### Breaking Changes", 2);
            sb.Append("* a breaking change feature", 2);
            Assert.Equal(sb.Build(), markdown);
        }

        public void Dispose()
        {
            Cleanup.DeleteDirectory(_testDirectory);
        }
    }
}
