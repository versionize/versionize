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
        TempCsProject.Create(_testSetup.WorkingDirectory, "1.1.0");

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldRunVersionizeWithDryRunOption()
    {
        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Formmatters.ShouldContain(formatter => formatter.Any(f => "bumping version from 1.1.0 to 1.1.0 in projects".Equals(f.Target)));
    }

    [Fact]
    public void ShouldVersionizeDesiredReleaseVersion()
    {
        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "--dry-run", "--skip-dirty", "--release-as", "2.0.0" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Formmatters.ShouldContain(formatter => formatter.Any(f => "bumping version from 1.1.0 to 2.0.0 in projects".Equals(f.Target)));
    }

    [Fact]
    public void ShouldPrintTheCurrentVersionWithInspectCommand()
    {
        var exitCode = Program.Main(new[] { "--workingDir", _testSetup.WorkingDirectory, "inspect" });

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].Message.ShouldBe("1.1.0");
    }

    [Fact]
    public void ShouldReadConfigurationFromConfigFile()
    {
        TempCsProject.Create(_testSetup.WorkingDirectory);

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"), @"{ ""skipDirty"": true }");

        var exitCode = Program.Main(new[] { "-w", _testSetup.WorkingDirectory });

        exitCode.ShouldBe(0);
        File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md")).ShouldBeTrue();
        _testSetup.Repository.Commits.Count().ShouldBe(1);
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
