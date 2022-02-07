using System.Collections.Generic;
using NuGet.Versioning;
using Shouldly;
using Xunit;

namespace Versionize.Tests
{
    public class VersionIncrementStrategyTests
    {
        [Fact]
        public void ShouldIncrementPatchVersionForEmptyCommits()
        {
            var strategy = new VersionIncrementStrategy(new List<ConventionalCommit>());
            strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 1, 2));
        }

        [Fact]
        public void ShouldNotIncrementPatchVersionForEmptyCommitsIfIgnoreInsignificantIsGiven()
        {
            var strategy = new VersionIncrementStrategy(new List<ConventionalCommit>());
            strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 1, 1));
        }

        [Fact]
        public void ShouldNotIncrementPatchVersionForInsignificantCommitsIfIgnoreInsignificantIsGiven()
        {
            var strategy = new VersionIncrementStrategy(new List<ConventionalCommit>
            {
                new ConventionalCommit { Type = "chore" }
            });

            strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 1, 1));
        }

        [Fact]
        public void ShouldIncrementPatchVersionForFixCommitsIfIgnoreInsignificantIsGiven()
        {
            var strategy = new VersionIncrementStrategy(new List<ConventionalCommit>
            {
                new ConventionalCommit { Type = "fix" }
            });

            strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 1, 2));
        }

        [Fact]
        public void ShouldIncrementMinorVersionForFeatures()
        {
            var strategy = new VersionIncrementStrategy(new List<ConventionalCommit>
            {
                new ConventionalCommit { Type = "feat" }
            });

            strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 2, 0));
        }

        [Fact]
        public void ShouldIncrementMajorVersionForBreakingChanges()
        {
            var strategy = new VersionIncrementStrategy(new List<ConventionalCommit>
            {
                new ConventionalCommit
                {
                    Type = "chore",
                    Notes = new List<ConventionalCommitNote>
                    {
                        new ConventionalCommitNote { Title = "BREAKING CHANGE"}
                    }
                }
            });

            strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(2, 0, 0));
        }

        [Theory]
        [MemberData(nameof(StableToPreRelease))]
        public void ShouldIncrementVersionFromStableToPreRelease(TestScenario testScenario)
        {
            var strategy = new VersionIncrementStrategy(testScenario.Commits);

            var nextVersion = strategy.NextVersion(testScenario.FromVersion, testScenario.PreReleaseLabel);

            nextVersion.ShouldBe(testScenario.ExpectedVersion);
        }


        [Theory]
        [MemberData(nameof(PreReleaseToPreRelease))]
        public void ShouldIncrementVersionFromPreReleaseToPreRelease(TestScenario testScenario)
        {
            var strategy = new VersionIncrementStrategy(testScenario.Commits);

            var nextVersion = strategy.NextVersion(testScenario.FromVersion, testScenario.PreReleaseLabel);

            nextVersion.ShouldBe(testScenario.ExpectedVersion);
        }

        [Theory]
        [MemberData(nameof(PreReleaseToStable))]
        public void ShouldIncrementVersionFromPreReleaseToStable(TestScenario testScenario)
        {
            var strategy = new VersionIncrementStrategy(testScenario.Commits);

            var nextVersion = strategy.NextVersion(testScenario.FromVersion, testScenario.PreReleaseLabel);

            nextVersion.ShouldBe(testScenario.ExpectedVersion);
        }

        public static IEnumerable<object[]> StableToPreRelease()
        {
            // From stable release to pre-release
            yield return Scenario("major update to 2.0.0-alpha.0")
                .FromVersion("1.0.0")
                .GivenCommit("feat", "BREAKING CHANGE")
                .PreRelease("alpha")
                .ExpectVersion("2.0.0-alpha.0");

            yield return Scenario("minor update to 1.1.0-alpha.0")
                .FromVersion("1.0.0")
                .GivenCommit("feat")
                .PreRelease("alpha")
                .ExpectVersion("1.1.0-alpha.0");

            yield return Scenario("fix update to 1.0.1-alpha.0")
                .FromVersion("1.0.0")
                .GivenCommit("fix")
                .PreRelease("alpha")
                .ExpectVersion("1.0.1-alpha.0");
        }

        public static IEnumerable<object[]> PreReleaseToPreRelease()
        {
            yield return Scenario("should increment pre-release release label version for breaking changes")
                .FromVersion("1.0.0-alpha.0")
                .GivenCommit("feat", "BREAKING CHANGE")
                .PreRelease("alpha")
                .ExpectVersion("1.0.0-alpha.1");

            yield return Scenario("should increment to major version for breaking changes in minor pre-release version")
                .FromVersion("1.1.0-alpha.0")
                .GivenCommit("feat", "BREAKING CHANGE")
                .PreRelease("alpha")
                .ExpectVersion("2.0.0-alpha.0");

            yield return Scenario("should not increase major version for breaking changes in pre-release version on major pre-release versions")
                .FromVersion("2.0.0-alpha.0")
                .GivenCommit("feat", "BREAKING CHANGE")
                .PreRelease("alpha")
                .ExpectVersion("2.0.0-alpha.1");

            yield return Scenario("should increment pre-release version for patch commits in same minor pre-release")
                .FromVersion("1.1.0-alpha.0")
                .GivenCommit("fix")
                .PreRelease("alpha")
                .ExpectVersion("1.1.0-alpha.1");

            yield return Scenario("should increase pre-release version for feat commits in same minor pre-release")
                .FromVersion("1.1.0-alpha.0")
                .GivenCommit("feat")
                .PreRelease("alpha")
                .ExpectVersion("1.1.0-alpha.1");

            yield return Scenario("should not increment pre-release version for insignificant commits")
                .FromVersion("1.0.0-alpha.0")
                .GivenCommit("chore")
                .PreRelease("alpha")
                .ExpectVersion("1.0.0-alpha.0");
        }

        public static IEnumerable<object[]> PreReleaseToStable()
        {
            // Release pre-releases from pre-release versions in next pre-release label
            yield return Scenario("should increment from alpha to beta including minor for version changes")
                .FromVersion("1.0.0-alpha.0")
                .GivenCommit("feat")
                .PreRelease("beta")
                .ExpectVersion("1.1.0-beta.0");

            yield return Scenario("should increment from alpha to beta in same major pre-release track")
                .FromVersion("1.0.0-alpha.0")
                .GivenCommit("fix")
                .PreRelease("beta")
                .ExpectVersion("1.0.0-beta.0");
        }

        public static TestScenarioBuilder Scenario(string description)
        {
            var builder = new TestScenarioBuilder();
            return builder.DescribedBy(description);
        }

        public class TestScenarioBuilder
        {
            private readonly List<ConventionalCommit> _commits = new();
            private SemanticVersion _expectedVersion;
            private string _preReleaseLabel;
            private SemanticVersion _fromVersion;
            private string _description;

            public TestScenarioBuilder FromVersion(string fromVersion)
            {
                _fromVersion = SemanticVersion.Parse(fromVersion);
                return this;
            }

            public TestScenarioBuilder GivenCommits(params ConventionalCommit[] commits)
            {
                _commits.AddRange(commits);
                return this;
            }

            public TestScenarioBuilder GivenCommit(string type, string note = null)
            {
                _commits.Add(new ConventionalCommit
                {
                    Type = type,
                    Notes = string.IsNullOrWhiteSpace(note) ? new List<ConventionalCommitNote>() : new List<ConventionalCommitNote>
                            {
                                new ConventionalCommitNote { Title = note }
                            }
                });

                return this;
            }

            public TestScenarioBuilder PreRelease(string preReleaseLabel = null)
            {
                _preReleaseLabel = preReleaseLabel;
                return this;
            }

            public TestScenarioBuilder DescribedBy(string description)
            {
                _description = description;
                return this;
            }

            public object[] ExpectVersion(string expectedVersion)
            {
                _expectedVersion = SemanticVersion.Parse(expectedVersion);

                return new object[] {
                    new TestScenario
                    {
                        Commits = _commits,
                        ExpectedVersion = _expectedVersion,
                        FromVersion = _fromVersion,
                        PreReleaseLabel = _preReleaseLabel,
                        Description = _description,
                    }
                };
            }
        }

        public class TestScenario
        {
            public List<ConventionalCommit> Commits { get; set; }
            public SemanticVersion ExpectedVersion { get; set; }
            public string PreReleaseLabel { get; set; }
            public SemanticVersion FromVersion { get; set; }

            public string Description { get; set; }

            public override string ToString()
            {
                return Description;
            }
        }
    }
}
