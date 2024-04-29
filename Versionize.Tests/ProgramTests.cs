using System.Text;
using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
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
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory, "1.1.0");

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldRunVersionizeWithDryRunOption()
    {
        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.1.0 to 1.1.0 in projects");
    }

    [Fact]
    public void ShouldVersionizeDesiredReleaseVersion()
    {
        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty", "--release-as", "2.0.0" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldContain("√ bumping version from 1.1.0 to 2.0.0 in projects");
    }

    [Fact]
    public void ShouldPrintTheCurrentVersionWithInspectCommand()
    {
        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "inspect" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe("1.1.0");
    }

    [Fact]
    public void ShouldReadConfigurationFromConfigFile()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"), @"{ ""skipDirty"": true }");

        var exitCode = Program.Main(new[] { "-w", _testSetup.WorkingDirectory });

        exitCode.ShouldBe(0);
        File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldBeTrue();
        _testSetup.Repository.Commits.Count().ShouldBe(1);
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
}
