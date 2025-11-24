using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json;
using Shouldly;
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
    public void ShouldPrintTheCurrentVersionWithInspectCommand()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "inspect"]);

        // Assert
        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe("1.1.0");
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
    public void ShouldPrintTheCurrentMonoRepoVersionWithInspectCommand()
    {
        var projects = new[]
        {
            (
                new ProjectOptions
                {
                    Name = "Project1",
                    Path = "project1",
                },
                "1.2.3"),
            (
                new ProjectOptions
                {
                    Name = "Project2",
                    Path = "project2"
                },
                "3.2.1")
        };

        var config = new FileConfig
        {
            Projects = [.. projects.Select(x => x.Item1)]
        };

        File.WriteAllText(
            Path.Join(_testSetup.WorkingDirectory, ".versionize"),
            JsonConvert.SerializeObject(config));

        foreach (var (project, version) in projects)
        {
            TempProject.CreateCsharpProject(
                Path.Combine(_testSetup.WorkingDirectory, project.Path),
                version);
        }

        foreach (var (project, version) in projects)
        {
            var output = new TestPlatformAbstractions();
            CommandLineUI.Platform = output;

            // Act
            var exitCode = Program.Main(
                [
                    "--workingDir", _testSetup.WorkingDirectory,
                    "--proj-name", project.Name,
                    "inspect"
                ]);

            // Assert
            exitCode.ShouldBe(0);
            output.Messages.ShouldHaveSingleItem();
            output.Messages[0].ShouldBe(version);
        }
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
    public void ChangelogCmd_OnlyIncludesChangesForSpecifiedVersion()
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

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
