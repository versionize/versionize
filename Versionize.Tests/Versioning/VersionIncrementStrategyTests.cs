using NuGet.Versioning;
using Shouldly;
using Versionize.ConventionalCommits;
using Xunit;

namespace Versionize.Versioning;

public class VersionIncrementStrategyTests
{
    [Fact]
    public void ShouldNotIncrementPatchVersionForEmptyCommitsIfIgnoreInsignificantIsGiven()
    {
        var strategy = new VersionIncrementStrategy([]);
        strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 1, 1));
    }

    [Fact]
    public void ShouldNotIncrementPatchVersionForInsignificantCommitsIfIgnoreInsignificantIsGiven()
    {
        var strategy = new VersionIncrementStrategy(
        [
            new() { Type = "chore" }
        ]);

        strategy.NextVersion(new SemanticVersion(1, 1, 1), null, false).ShouldBe(new SemanticVersion(1, 1, 1));
    }

    [Fact]
    public void ShouldIncrementPatchVersionForFixCommitsIfIgnoreInsignificantIsGiven()
    {
        var strategy = new VersionIncrementStrategy(
        [
            new() { Type = "fix" }
        ]);

        strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 1, 2));
    }

    [Fact]
    public void ShouldIncrementMinorVersionForFeatures()
    {
        var strategy = new VersionIncrementStrategy(
        [
            new() { Type = "feat" }
        ]);

        strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(1, 2, 0));
    }

    [Fact]
    public void ShouldIncrementMajorVersionForBreakingChanges()
    {
        var strategy = new VersionIncrementStrategy(
        [
            new() {
                Type = "chore",
                Notes =
                [
                    new ConventionalCommitNote { Title = "BREAKING CHANGE"}
                ]
            }
        ]);

        strategy.NextVersion(new SemanticVersion(1, 1, 1)).ShouldBe(new SemanticVersion(2, 0, 0));
    }

    [Theory]
    [MemberData(nameof(StableToPrerelease))]
    public void ShouldIncrementVersionFromStableToPrerelease(TestScenario testScenario)
    {
        var strategy = new VersionIncrementStrategy(testScenario.Commits);

        var nextVersion = strategy.NextVersion(testScenario.FromVersion, testScenario.PrereleaseLabel);

        nextVersion.ShouldBe(testScenario.ExpectedVersion);
    }


    [Theory]
    [MemberData(nameof(PrereleaseToPrerelease))]
    public void ShouldIncrementVersionFromPrereleaseToPrerelease(TestScenario testScenario)
    {
        var strategy = new VersionIncrementStrategy(testScenario.Commits);

        var nextVersion = strategy.NextVersion(testScenario.FromVersion, testScenario.PrereleaseLabel, !testScenario.IgnoreInsignificantCommits);

        nextVersion.ShouldBe(testScenario.ExpectedVersion);
    }

    [Theory]
    [MemberData(nameof(PrereleaseToStable))]
    public void ShouldIncrementVersionFromPrereleaseToStable(TestScenario testScenario)
    {
        var strategy = new VersionIncrementStrategy(testScenario.Commits);

        var nextVersion = strategy.NextVersion(testScenario.FromVersion, testScenario.PrereleaseLabel);

        nextVersion.ShouldBe(testScenario.ExpectedVersion);
    }

    public static IEnumerable<object[]> StableToPrerelease()
    {
        // From stable release to pre-release
        yield return Scenario("release number increment from major with breaking change commit to major alpha")
            .FromVersion("1.0.0")
            .GivenCommit("feat", "BREAKING CHANGE")
            .Prerelease("alpha")
            .ExpectVersion("2.0.0-alpha.0");

        yield return Scenario("release number increment from major with feat commit to minor alpha")
            .FromVersion("1.0.0")
            .GivenCommit("feat")
            .Prerelease("alpha")
            .ExpectVersion("1.1.0-alpha.0");

        yield return Scenario("release number increment from major with fix commit to patch alpha")
            .FromVersion("1.0.0")
            .GivenCommit("fix")
            .Prerelease("alpha")
            .ExpectVersion("1.0.1-alpha.0");

        yield return Scenario("release number increment from major with chore commit to patch alpha")
            .FromVersion("1.0.0")
            .GivenCommit("chore")
            .Prerelease("alpha")
            .ExpectVersion("1.0.1-alpha.0");
    }

    public static IEnumerable<object[]> PrereleaseToPrerelease()
    {
        yield return Scenario("pre-release number increment from major with breaking change commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("feat", "BREAKING CHANGE")
            .Prerelease("alpha")
            .ExpectVersion("1.0.0-alpha.1");

        yield return Scenario("version increment from minor to major with breaking change commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("feat", "BREAKING CHANGE")
            .Prerelease("alpha")
            .ExpectVersion("2.0.0-alpha.0");

        yield return Scenario("pre-release number increment from minor with fix commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("fix")
            .Prerelease("alpha")
            .ExpectVersion("1.1.0-alpha.1");

        yield return Scenario("pre-release number increment from minor with feat commit")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("feat")
            .Prerelease("alpha")
            .ExpectVersion("1.1.0-alpha.1");

        yield return Scenario("ignore insignificant commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("chore")
            .Prerelease("alpha")
            .IgnoreInsignificantCommits()
            .ExpectVersion("1.0.0-alpha.0");

        yield return Scenario("pre-release number increment from minor with fix commit with new pre-release label")
            .FromVersion("1.1.0-alpha.0")
            .GivenCommit("fix")
            .Prerelease("beta")
            .ExpectVersion("1.1.0-beta.0");

        yield return Scenario("exit for lower version as existed commit")
            .FromVersion("1.0.0-alpha.0")
            .GivenCommit("chore")
            .Prerelease("alpha")
            .IgnoreInsignificantCommits()
            .ExpectVersion("1.0.0-alpha.0");
    }

    public static IEnumerable<object[]> PrereleaseToStable()
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
        private readonly List<ConventionalCommit> _commits = [];
        private SemanticVersion _expectedVersion;
        private string _prereleaseLabel;
        private SemanticVersion _fromVersion;
        private string _description;
        private bool _ignoreInsignificantCommits;

        public TestScenarioBuilder FromVersion(string version)
        {
            _fromVersion = SemanticVersion.Parse(version);
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
                Notes = string.IsNullOrWhiteSpace(note) ? [] :
                        [
                            new ConventionalCommitNote { Title = note }
                        ]
            });

            return this;
        }

        public TestScenarioBuilder Prerelease(string prereleaseLabel = null)
        {
            _prereleaseLabel = prereleaseLabel;
            return this;
        }

        public TestScenarioBuilder DescribedBy(string description)
        {
            _description = description;
            return this;
        }

        public TestScenarioBuilder IgnoreInsignificantCommits()
        {
            _ignoreInsignificantCommits = true;
            return this;
        }

        public object[] ExpectVersion(string expectedVersion)
        {
            _expectedVersion = SemanticVersion.Parse(expectedVersion);

            return Build();
        }

        public object[] Build()
        {
            return [
                new TestScenario
                {
                    Commits = _commits,
                    ExpectedVersion = _expectedVersion,
                    FromVersion = _fromVersion,
                    PrereleaseLabel = _prereleaseLabel,
                    Description = _description,
                    IgnoreInsignificantCommits = _ignoreInsignificantCommits,
                }
            ];
        }
    }

    public class TestScenario
    {
        public List<ConventionalCommit> Commits { get; set; }
        public SemanticVersion ExpectedVersion { get; set; }
        public string PrereleaseLabel { get; set; }
        public SemanticVersion FromVersion { get; set; }

        public string Description { get; set; }
        public bool IgnoreInsignificantCommits { get; internal set; }

        public override string ToString()
        {
            return Description;
        }
    }
}
