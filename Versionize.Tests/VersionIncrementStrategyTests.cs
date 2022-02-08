using NuGet.Versioning;
using Shouldly;
using Xunit;

namespace Versionize.Tests;

public class VersionIncrementStrategyTests
{
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
        yield return Scenario("release number increment from major with breaking change commit to major alpha")
            .FromVersion("1.0.0")
            .GivenCommit("feat", "BREAKING CHANGE")
            .PreRelease("alpha")
            .ExpectVersion("2.0.0-alpha.0");

        yield return Scenario("release number increment from major with feat commit to minor alpha")
            .FromVersion("1.0.0")
            .GivenCommit("feat")
            .PreRelease("alpha")
            .ExpectVersion("1.1.0-alpha.0");

        yield return Scenario("release number increment from major with fix commit to patch alpha")
            .FromVersion("1.0.0")
            .GivenCommit("fix")
            .PreRelease("alpha")
            .ExpectVersion("1.0.1-alpha.0");
    }

    public static IEnumerable<object[]> PreReleaseToPreRelease()
    {
        yield return Scenario("pre-release number increment from major with breaking change commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("feat", "BREAKING CHANGE")
            .PreRelease("alpha")
            .ExpectVersion("1.0.0-alpha.1");

        yield return Scenario("version increment from minor to major with breaking change commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("feat", "BREAKING CHANGE")
            .PreRelease("alpha")
            .ExpectVersion("2.0.0-alpha.0");

        yield return Scenario("pre-release number increment from minor with fix commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("fix")
            .PreRelease("alpha")
            .ExpectVersion("1.1.0-alpha.1");

        yield return Scenario("pre-release number increment from minor with feat commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("feat")
            .PreRelease("alpha")
            .ExpectVersion("1.1.0-alpha.1");

        yield return Scenario("ignore insignificant commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("chore")
            .PreRelease("alpha")
            .ExpectVersion("1.0.0-alpha.0");

        yield return Scenario("pre-release number increment from minor with fix commit with new pre-release label")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("fix")
            .PreRelease("beta")
            .ExpectVersion("1.1.0-beta.0");

        yield return Scenario("exit for lower version as existed commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("chore")
            .PreRelease("alpha")
            .ExpectVersion("1.0.0-alpha.0");
    }

    public static IEnumerable<object[]> PreReleaseToStable()
    {
        // Release pre-releases from pre-release versions in next pre-release label
        yield return Scenario("release from major pre-release with feat commit")
            .FromVersion("2.0.0-alpha.2")
            .GivenCommit("feat")
            .ExpectVersion("2.0.0");

        yield return Scenario("release from major pre-release with fix commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("fix")
            .ExpectVersion("1.0.0");

        yield return Scenario("release from minor pre-release with breaking change commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("fix", "BREAKING CHANGE")
            .ExpectVersion("2.0.0");

        yield return Scenario("release from patch pre-release with feat commit")
            .FromVersion("1.0.1-alpha.0")
            .GivenCommit("feat")
            .ExpectVersion("1.1.0");
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

            return Build();
        }

        public object[] Build()
        {
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
