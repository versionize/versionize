using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json;
using Shouldly;
using Versionize.BumpFiles;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public class ProgramTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;
    private static readonly string[] sourceArray =
        [
            "feat(git-case): subject text",
            "Merged PR 123: fix(squash-azure-case): subject text #64",
            "Pull Request 11792: feat(azure-case): subject text"
        ];

    public ProgramTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldRunVersionizeWithDryRunOption()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty"]);

        // Assert
        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.1.0 to 1.1.0 in projects");
    }

    [Fact]
    public void ShouldPerformADryRun()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
        GitTestHelpers.CommitAll(_testSetup.Repository, "feat: first commit");

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty"]);

        // Assert
        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.Count.ShouldBe(7);
        _testPlatformAbstractions.Messages[0].ShouldBe("Discovered 1 versionable projects");
        _testPlatformAbstractions.Messages[3].ShouldBe("\n---");
        _testPlatformAbstractions.Messages[4].ShouldContain("* first commit");
        _testPlatformAbstractions.Messages[5].ShouldBe("---\n");
        var wasChangelogWritten = File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md"));
        Assert.False(wasChangelogWritten);
    }

    [Fact]
    public void ShouldVersionizeDesiredReleaseVersion()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty", "--release-as", "2.0.0"]);

        // Assert
        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.1.0 to 2.0.0 in projects");
    }

    [Fact]
    public void ShouldReadConfigurationFromConfigFile()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var config = new FileConfig
        {
            SkipDirty = true,
            Changelog = new ChangelogOptions
            {
                Header = "My Custom header"
            }
        };

        var json = JsonConvert.SerializeObject(config);

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"), json);

        // Act
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory]);

        // Assert
        exitCode.ShouldBe(0);
        File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldBeTrue();
        File.ReadAllText(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldContain("My Custom header");
        _testSetup.Repository.Commits.Count().ShouldBe(1);
    }

    [Fact]
    public void ShouldReadConfigurationFromConfigFileInCustomDirectory()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var config = new FileConfig
        {
            SkipDirty = true,
            Changelog = new ChangelogOptions
            {
                Header = "My Custom header"
            }
        };

        var json = JsonConvert.SerializeObject(config);
        var configDir = Path.Join(_testSetup.WorkingDirectory, "..");

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "..", ".versionize"), json);

        // Act
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--configDir", configDir, "--skip-dirty"]);

        // Assert
        exitCode.ShouldBe(0);
        File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldBeTrue();
        File.ReadAllText(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldContain("My Custom header");
        _testSetup.Repository.Commits.Count().ShouldBe(1);
    }

    [Fact]
    public void ShouldSupportMonoRepo()
    {
        var projects = new[]
        {
            new ProjectOptions
            {
                Name = "Project1",
                Path = "project1",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project1 header"
                }
            },
            new ProjectOptions
            {
                Name = "Project2",
                Path = "project2"
            }
        };

        var config = new FileConfig
        {
            Projects = projects,
            Changelog = new ChangelogOptions
            {
                Header = "Default custom header"
            }
        };

        File.WriteAllText(
            Path.Join(_testSetup.WorkingDirectory, ".versionize"),
            JsonConvert.SerializeObject(config));

        var fileCommitter = new FileCommitter(_testSetup);

        foreach (var project in projects)
        {
            TempProject.CreateCsharpProject(
                Path.Combine(_testSetup.WorkingDirectory, project.Path));


            var commitMessages = new[] { $"feat: new feature at {project.Name}" };
            foreach (var commitMessage in commitMessages)
            {
                fileCommitter.CommitChange(commitMessage, project.Path);
            }
        }

        foreach (var project in projects)
        {
            var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--proj-name", project.Name]);
            exitCode.ShouldBe(0);

            var changelogFile = Path.Join(_testSetup.WorkingDirectory, project.Path, "CHANGELOG.md");
            File.Exists(changelogFile).ShouldBeTrue();

            var changelog = File.ReadAllText(changelogFile, Encoding.UTF8);

            changelog.ShouldStartWith(project.Changelog.Header ??
                                      config.Changelog?.Header ??
                                      ChangelogOptions.Default.Header);

            foreach (var checkPName in projects.Select(x => x.Name))
            {
                var commitMessages = new[] { $"new feature at {checkPName}" };
                foreach (var commitMessage in commitMessages)
                {
                    if (checkPName == project.Name)
                    {
                        changelog.ShouldContain(commitMessage);
                    }
                    else
                    {
                        changelog.ShouldNotContain(commitMessage);
                    }
                }
            }
        }
    }

    [Fact]
    public void ShouldExtraCommitHeaderPatternOptionsFromConfigFile()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        File.WriteAllText(
            Path.Join(_testSetup.WorkingDirectory, ".versionize"), """
            {
              "CommitParser":{
                  "HeaderPatterns":[
                  "^Merged PR \\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$",
                  "^Pull Request \\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$"
                ]
              }
            }
            """);

        _ = sourceArray.Select(
                x => _testSetup.Repository.Commit(x,
                    new Signature("versionize", "test@versionize.com", DateTimeOffset.Now),
                    new Signature("versionize", "test@versionize.com", DateTimeOffset.Now),
                    new CommitOptions { AllowEmptyCommit = true }))
            .ToArray();

        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--skip-dirty"]);

        exitCode.ShouldBe(0);
        _testSetup.Repository.Commits.Count().ShouldBe(4);

        var changelogFile = Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md");
        File.Exists(changelogFile).ShouldBeTrue();

        var changelog = File.ReadAllText(changelogFile, Encoding.UTF8);
        changelog.ShouldContain("git-case");
        changelog.ShouldContain("squash-azure-case");
        changelog.ShouldContain("azure-case");
    }

    [Fact]
    public void ShouldExitIfProjectsUseInconsistentNaming()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project2"), "2.0.0");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        // Act
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory]);

        // Assert
        exitCode.ShouldBe(1);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe(ErrorMessages.InconsistentProjectVersions(_testSetup.WorkingDirectory, "Version"));
    }

    [Fact]
    public void ShouldReleaseAsSpecifiedVersion()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        // Act
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--release-as", "2.0.0"]);

        // Assert
        exitCode.ShouldBe(0);
        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(["v2.0.0"]);
    }

    [Fact]
    public void ShouldEmitAUsefulErrorMessageForDuplicateTags()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
        GitTestHelpers.CommitAll(_testSetup.Repository);
        _ = _testSetup.Repository.ApplyTag("v2.0.0");

        // Act
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--release-as", "2.0.0"]);

        // Assert
        exitCode.ShouldBe(1);
        _testPlatformAbstractions.Messages.Last().ShouldBe(ErrorMessages.VersionAlreadyExists("2.0.0"));
    }

    [Fact]
    public void ShouldExitIfReleaseAsSpecifiedVersionIsInvalid()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        // Act
        var exitCode = Program.Main(["-w", _testSetup.WorkingDirectory, "--release-as", "kanguru"]);

        // Assert
        exitCode.ShouldBe(1);

        // Caught by McMaster.Extensions.CommandLineUtils validation.
        _testPlatformAbstractions.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldIgnoreInsignificantCommits()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        // Run versionize
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);
        _testSetup.Repository.Head.Tip.Message.TrimEnd().ShouldBe("chore(release): 1.0.0");

        // Add insignificant change
        File.AppendAllText(workingFilePath, "This is another line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository, "chore: Added line of text");

        // Get last commit
        var lastCommit = _testSetup.Repository.Head.Tip;

        // Act
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--ignore-insignificant-commits"]);

        // Assert
        exitCode.ShouldBe(0);
        lastCommit.ShouldBe(_testSetup.Repository.Head.Tip);
    }

    [Fact]
    public void ShouldExitWithNonZeroExitCodeForInsignificantCommits()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version first
        fileCommitter.CommitChange("chore: initial commit");

        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);
        _testSetup.Repository.Head.Tip.Message.TrimEnd().ShouldBe("chore(release): 1.0.0");
        _testPlatformAbstractions.Messages.Clear();

        // Insignificant change release
        fileCommitter.CommitChange("chore: insignificant change");

        // Act
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--exit-insignificant-commits"]);

        // Assert
        exitCode.ShouldBe(1);
        // First 2 messages are about project discovery
        _testPlatformAbstractions.Messages.Count.ShouldBe(3);
        _testPlatformAbstractions.Messages[2].ShouldBe(ErrorMessages.VersionUnaffected("1.0.0"));
    }

    [Fact]
    public void ShouldAddSuffixToReleaseCommitMessage()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        // Create and commit a test file
        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");
        File.WriteAllText(workingFilePath, "First line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        const string suffix = "[skip ci]";

        // Act
        var exitCode = Program.Main(
        [
            "--workingDir", _testSetup.WorkingDirectory,
            "--commit-suffix", suffix
        ]);

        // Assert
        exitCode.ShouldBe(0);
        var lastCommit = _testSetup.Repository.Head.Tip;
        lastCommit.Message.ShouldContain(suffix);
    }

    [Fact]
    public void ShouldPrereleaseToCurrentMaximumPrereleaseVersion()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: feature pre-release");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha"]);
        exitCode.ShouldBe(0);

        // Prerelease as major alpha
        fileCommitter.CommitChange("chore: initial commit\n\nBREAKING CHANGE: This is a breaking change");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha"]);
        exitCode.ShouldBe(0);

        var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
        versionTagNames.ShouldBe(["v1.0.0", "v1.1.0-alpha.0", "v2.0.0-alpha.0"]);
    }

    [Fact]
    public void ShouldExitForInvalidPrereleaseSequences()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Prerelease a minor beta
        fileCommitter.CommitChange("feat: feature pre-release");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "beta"]);
        exitCode.ShouldBe(0);

        // Try Prerelease a minor alpha
        fileCommitter.CommitChange("feat: feature pre-release");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha"]);
        exitCode.ShouldBe(1);
        _testPlatformAbstractions.Messages.Last()
            .ShouldBe(ErrorMessages.SemanticVersionConflict(next: "1.1.0-alpha.0", current: "1.1.0-beta.0"));
    }

    [Fact]
    public void ShouldExitForInvalidReleaseAsReleases()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Release as lower than current version
        fileCommitter.CommitChange("feat: some feature");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--release-as", "0.9.0"]);
        exitCode.ShouldBe(1);
        _testPlatformAbstractions.Messages.Last()
            .ShouldBe(ErrorMessages.SemanticVersionConflict(next: "0.9.0", current: "1.0.0"));
    }

    [Fact]
    public void ShouldWriteFirstParentOnlyCommit()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("feat: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--first-parent-only-commits"]);
        exitCode.ShouldBe(0);

        var defaultBranch = _testSetup.Repository.Branches.First();
        var featBranch = _testSetup.Repository.CreateBranch("feature/new-feature");
        LibGit2Sharp.Commands.Checkout(_testSetup.Repository, featBranch);

        // Prerelease as patch alpha
        fileCommitter.CommitChange("feat: add something on branch");
        fileCommitter.CommitChange("feat: add something else on branch");
        fileCommitter.CommitChange("feat: last add on branch");

        var author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
        LibGit2Sharp.Commands.Checkout(_testSetup.Repository, defaultBranch);
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--first-parent-only-commits"]);
        exitCode.ShouldBe(0);
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--first-parent-only-commits"]);
        exitCode.ShouldBe(0);
        _testSetup.Repository.Merge(featBranch, author, new MergeOptions
        {
            CommitOnSuccess = true,
        });

        fileCommitter.CommitChange("feat: new feature on file");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--first-parent-only-commits"]);
        exitCode.ShouldBe(0);

        var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
        versionTagNames.ShouldBe(["v1.0.0", "v1.0.1", "v1.0.2", "v1.1.0"]);

        var commitDate = DateTime.Now.ToString("yyyy-MM-dd");
        var changelogContents = File.ReadAllText(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md"));
        var sb = new ChangelogStringBuilder();
        sb.Append(ChangelogOptions.Preamble);

        sb.Append("<a name=\"1.1.0\"></a>");
        sb.Append($"## 1.1.0 ({commitDate})", 2);
        sb.Append("### Features", 2);
        sb.Append("* new feature on file", 2);

        sb.Append("<a name=\"1.0.2\"></a>");
        sb.Append($"## 1.0.2 ({commitDate})", 2);

        sb.Append("<a name=\"1.0.1\"></a>");
        sb.Append($"## 1.0.1 ({commitDate})", 2);

        sb.Append("<a name=\"1.0.0\"></a>");
        sb.Append($"## 1.0.0 ({commitDate})", 2);
        sb.Append("### Features", 2);
        sb.Append("* initial commit", 2);

        var expected = sb.Build();

        Assert.Equal(expected, changelogContents);
    }

    [Fact]
    public void ShouldAggregatePrereleases()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("feat: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // Prerelease as patch alpha
        fileCommitter.CommitChange("fix: a fix");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // Full release
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // Full release
        fileCommitter.CommitChange("feat: another feature");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
        versionTagNames.ShouldBe(["v1.0.0", "v1.0.1-alpha.0", "v1.1.0", "v1.1.0-alpha.0", "v1.2.0"]);

        var commitDate = DateTime.Now.ToString("yyyy-MM-dd");
        var changelogContents = File.ReadAllText(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md"));
        var sb = new ChangelogStringBuilder();
        sb.Append(ChangelogOptions.Preamble);

        sb.Append("<a name=\"1.2.0\"></a>");
        sb.Append($"## 1.2.0 ({commitDate})", 2);
        sb.Append("### Features", 2);
        sb.Append("* another feature", 2);

        sb.Append("<a name=\"1.1.0\"></a>");
        sb.Append($"## 1.1.0 ({commitDate})", 2);
        sb.Append("### Features", 2);
        sb.Append("* a feature", 2);
        sb.Append("### Bug Fixes", 2);
        sb.Append("* a fix", 2);

        sb.Append("<a name=\"1.1.0-alpha.0\"></a>");
        sb.Append($"## 1.1.0-alpha.0 ({commitDate})", 2);
        sb.Append("### Features", 2);
        sb.Append("* a feature", 2);
        sb.Append("### Bug Fixes", 2);
        sb.Append("* a fix", 2);

        sb.Append("<a name=\"1.0.1-alpha.0\"></a>");
        sb.Append($"## 1.0.1-alpha.0 ({commitDate})", 2);
        sb.Append("### Bug Fixes", 2);
        sb.Append("* a fix", 2);

        sb.Append("<a name=\"1.0.0\"></a>");
        sb.Append($"## 1.0.0 ({commitDate})", 2);
        sb.Append("### Features", 2);
        sb.Append("* initial commit", 2);

        var expected = sb.Build();

        Assert.Equal(expected, changelogContents);
    }

    [Fact]
    public void ShouldDisplayExpectedMessage_BumpingVersionFromXToY()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.0.0");
        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("feat: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Patch release
        fileCommitter.CommitChange("fix: a fix");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);

        // Assert
        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.0.0 to 1.0.1 in projects");
    }

    [Fact]
    public void ShouldFindReleaseCommitViaMessage()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("feat: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Prerelease as patch alpha
        fileCommitter.CommitChange("fix: a fix");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--skip-tag"]);
        exitCode.ShouldBe(0);

        // Prerelease as patch alpha
        fileCommitter.CommitChange("fix: another fix");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--skip-tag", "--find-release-commit-via-message"]);
        exitCode.ShouldBe(0);

        var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
        versionTagNames.ShouldBe(["v1.0.0"]);

        var projects = DotnetBumpFile.Create(_testSetup.WorkingDirectory);
        projects.Version.ToNormalizedString().ShouldBe("1.0.1-alpha.1");

        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.0.1-alpha.0 to 1.0.1-alpha.1 in projects");
    }

    [Fact]
    public void ShouldBumpConsecutivePreReleasesWhenUsingTagOnly()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("feat: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--tag-only"]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature 2");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--tag-only"]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature 3");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--tag-only"]);
        exitCode.ShouldBe(0);

        var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
        versionTagNames.ShouldBe(["v1.0.0", "v1.1.0-alpha.0", "v1.1.0-alpha.1", "v1.1.0-alpha.2"]);
    }

    [Fact]
    public void ShouldBumpConsecutivePreReleasesWhenAggregatingPrereleases()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("feat: initial commit");
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature 2");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: a feature 3");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--pre-release", "alpha", "--aggregate-pre-releases"]);
        exitCode.ShouldBe(0);

        var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
        string[] expected = ["v1.0.0", "v1.1.0-alpha.0", "v1.1.0-alpha.1", "v1.1.0-alpha.2"];
        versionTagNames.ShouldBe(expected);
    }

    [Fact]
    public void ShouldTagInitialVersionUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(1);
        var commitThatShouldBeTagged = _testSetup.Repository.Commits.First();

        _testSetup.Repository.Tags.Count().ShouldBe(1);
        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(["v1.0.0"]);
        var tag = _testSetup.Repository.Tags.First();
        tag.Annotation.Target.Sha.ShouldBe(commitThatShouldBeTagged.Sha);
    }

    [Fact]
    public void ShouldTagInitialVersionUsingTagOnlyWithNonTrackedCommits()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline");
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline2");
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline3");
        new FileCommitter(_testSetup).CommitChange("build: updated build pipeline4");

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testSetup.Repository.Commits.Count().ShouldBe(5);
        _testSetup.Repository.Tags.Count().ShouldBe(1);
        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(["v1.0.0"]);
    }

    [Fact]
    public void ShouldTagVersionAfterFeatUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        new FileCommitter(_testSetup).CommitChange("feat: first feature");

        // Act
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.1.0"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(2);
    }

    [Fact]
    public void ShouldTagVersionAfterFixUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        new FileCommitter(_testSetup).CommitChange("fix: first feature");

        // Act
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.0.1"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(2);
    }

    [Fact]
    public void ShouldTagVersionWhenMultipleCommitsInOneVersionUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        var fc = new FileCommitter(_testSetup);
        fc.CommitChange("fix: first fix");
        fc.CommitChange("fix: second fix");
        fc.CommitChange("feat: first feature");

        // Act
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.1.0"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(4);
    }

    [Fact]
    public void ShouldTagVersionAfterEachVersionizeCommandUsingTagOnly()
    {
        // Arrange
        GitTestHelpers.CommitAll(_testSetup.Repository);
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        new FileCommitter(_testSetup).CommitChange("fix: first fix");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Act
        new FileCommitter(_testSetup).CommitChange("fix: another fix");
        exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "--tag-only", "--skip-dirty", "--skip-changelog"]);
        exitCode.ShouldBe(0);

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(["v1.0.0", "v1.0.1", "v1.0.2"]);

        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(3);
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
