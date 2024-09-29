using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Tests;

public class ProgramTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public ProgramTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldRunVersionizeWithDryRunOption()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.1.0 to 1.1.0 in projects");
    }

    [Fact]
    public void ShouldVersionizeDesiredReleaseVersion()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty", "--release-as", "2.0.0" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.1.0 to 2.0.0 in projects");
    }

    [Fact]
    public void ShouldPrintTheCurrentVersionWithInspectCommand()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "inspect" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe("1.1.0");
    }

    [Fact]
    public void ShouldReadConfigurationFromConfigFile()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var config = new ConfigurationContract
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

        var exitCode = Program.Main(new[] { "-w", _testSetup.WorkingDirectory });

        exitCode.ShouldBe(0);
        File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldBeTrue();
        File.ReadAllText(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldContain("My Custom header");
        _testSetup.Repository.Commits.Count().ShouldBe(1);
    }

    [Fact]
    public void ShouldReadConfigurationFromConfigFileInCustomDirectory()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var config = new ConfigurationContract
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

        var exitCode = Program.Main(new[] { "-w", _testSetup.WorkingDirectory, "--configDir", configDir, "--skip-dirty" });

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

        var config = new ConfigurationContract
        {
            Projects = projects.Select(x => x.Item1).ToArray()
        };

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"),
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

            var exitCode = Program.Main(
                new[]
                {
                    "--workingDir", _testSetup.WorkingDirectory,
                    "--proj-name", project.Name,
                    "inspect"
                });

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

        var config = new ConfigurationContract
        {
            Projects = projects,
            Changelog = new ChangelogOptions
            {
                Header = "Default custom header"
            }
        };

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"),
            JsonConvert.SerializeObject(config));
        
        var fileCommitter = new FileCommitter(_testSetup);

        foreach (var project in projects)
        {
            TempProject.CreateCsharpProject(
                Path.Combine(_testSetup.WorkingDirectory, project.Path));

            foreach (var commitMessage in new[]
                     {
                         $"feat: new feature at {project.Name}"
                     })
            {
                fileCommitter.CommitChange(commitMessage, project.Path);
            }
        }

        foreach (var project in projects)
        {
            var exitCode = Program.Main(new[] {"-w", _testSetup.WorkingDirectory, "--proj-name", project.Name});
            exitCode.ShouldBe(0);

            var changelogFile = Path.Join(_testSetup.WorkingDirectory, project.Path, "CHANGELOG.md");
            File.Exists(changelogFile).ShouldBeTrue();

            var changelog = File.ReadAllText(changelogFile, Encoding.UTF8);

            changelog.ShouldStartWith(project.Changelog.Header ??
                                      config.Changelog?.Header ?? 
                                      ChangelogOptions.Default.Header);

            foreach (var checkPName in projects.Select(x => x.Name))
            {
                foreach (var commitMessage in new[]
                         {
                             $"new feature at {checkPName}"
                         })
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

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"),
            @"
{
  ""CommitParser"":{
    ""HeaderPatterns"":[
      ""^Merged PR \\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$"",
      ""^Pull Request \\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$""
    ]
  }
}
");
        
        _ = new[]
            {
                "feat(git-case): subject text",
                "Merged PR 123: fix(squash-azure-case): subject text #64",
                "Pull Request 11792: feat(azure-case): subject text"
            }
            .Select(
                x => _testSetup.Repository.Commit(x,
                    new Signature("versionize", "test@versionize.com", DateTimeOffset.Now),
                    new Signature("versionize", "test@versionize.com", DateTimeOffset.Now),
                    new CommitOptions { AllowEmptyCommit = true }))
            .ToArray();
        
        var exitCode = Program.Main(new[] { "-w", _testSetup.WorkingDirectory, "--skip-dirty" });

        exitCode.ShouldBe(0);
        _testSetup.Repository.Commits.Count().ShouldBe(4);

        var changelogFile = Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md");
        File.Exists(changelogFile).ShouldBeTrue();

        var changelog = File.ReadAllText(changelogFile, Encoding.UTF8);
        changelog.ShouldContain("git-case");
        changelog.ShouldContain("squash-azure-case");
        changelog.ShouldContain("azure-case");
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    private static void CommitAll(IRepository repository, string message = "feat: Initial commit")
    {
        var author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
        Commands.Stage(repository, "*");
        repository.Commit(message, author, author);
    }

    class FileCommitter
    {
        private readonly TestSetup _testSetup;

        public FileCommitter(TestSetup testSetup)
        {
            _testSetup = testSetup;
        }

        public void CommitChange(string commitMessage, string changeOnDirectory = "")
        {
            var directory = Path.Join(_testSetup.WorkingDirectory, changeOnDirectory);
            Directory.CreateDirectory(directory);

            var workingFilePath = Path.Join(directory, "hello.txt");
            File.WriteAllText(workingFilePath, Guid.NewGuid().ToString());
            CommitAll(_testSetup.Repository, commitMessage);
        }
    }
}
