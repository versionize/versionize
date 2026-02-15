using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public class InitCommandTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;
    private const string ProjectContents = """
        <Project Sdk=\"Microsoft.NET.Sdk\">
            <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
            </PropertyGroup>
        </Project>
        """;

    public InitCommandTests()
    {
        _testSetup = TestSetup.Create();
        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldCreateConfigAndAddVersionElements()
    {
        var consoleDir = Path.Combine(_testSetup.WorkingDirectory, "src", "Console");
        var libraryDir = Path.Combine(_testSetup.WorkingDirectory, "src", "Library");

        TempProject.CreateFromProjectContents(consoleDir, "csproj", ProjectContents);
        TempProject.CreateFromProjectContents(libraryDir, "csproj", ProjectContents);

        var exitCode = Program.Main(["init", "--workingDir", _testSetup.WorkingDirectory]);

        exitCode.ShouldBe(0);

        var configPath = Path.Combine(_testSetup.WorkingDirectory, ".versionize");
        File.Exists(configPath).ShouldBeTrue();

        var config = FileConfig.Load(configPath);
        config.ShouldNotBeNull();
        config!.Projects.Length.ShouldBe(2);
        config.Projects.Select(p => p.Name).ShouldContain("console");
        config.Projects.Select(p => p.Name).ShouldContain("library");

        File.ReadAllText(Path.Combine(consoleDir, "Console.csproj"))
            .ShouldContain("<Version>0.0.0</Version>");
        File.ReadAllText(Path.Combine(libraryDir, "Library.csproj"))
            .ShouldContain("<Version>0.0.0</Version>");
    }

    [Fact]
    public void ShouldSkipConfigForSingleProjectByDefault()
    {
        var appDir = Path.Combine(_testSetup.WorkingDirectory, "src", "App");
        TempProject.CreateFromProjectContents(appDir, "csproj", ProjectContents);

        var exitCode = Program.Main(["init", "--workingDir", _testSetup.WorkingDirectory]);

        exitCode.ShouldBe(0);
        var configPath = Path.Combine(_testSetup.WorkingDirectory, ".versionize");
        File.Exists(configPath).ShouldBeFalse();
        File.ReadAllText(Path.Combine(appDir, "App.csproj"))
            .ShouldContain("<Version>0.0.0</Version>");
        _testPlatformAbstractions.Messages.ShouldContain("single project detected; no .versionize file created");
        _testPlatformAbstractions.Messages.ShouldContain("versionize can be used without any further configurations");
        _testPlatformAbstractions.Messages.ShouldContain("use --force-config to write a .versionize file anyway");
    }

    [Fact]
    public void ShouldCreateConfigForSingleProjectWhenForced()
    {
        var appDir = Path.Combine(_testSetup.WorkingDirectory, "src", "App");
        TempProject.CreateFromProjectContents(appDir, "csproj", ProjectContents);

        var exitCode = Program.Main([
            "init",
            "--workingDir",
            _testSetup.WorkingDirectory,
            "--force-config"]);

        exitCode.ShouldBe(0);
        var configPath = Path.Combine(_testSetup.WorkingDirectory, ".versionize");
        File.Exists(configPath).ShouldBeTrue();

        var config = FileConfig.Load(configPath);
        config.ShouldNotBeNull();
        config!.Projects.Length.ShouldBe(1);
        config.Projects[0].Name.ShouldBe("app");
    }

    [Fact]
    public void ShouldReportInvalidVersionValuesInMultiProjectSetup()
    {
        var invalidProjectContents = """
            <Project Sdk=\"Microsoft.NET.Sdk\">
                <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <Version>invalid</Version>
                </PropertyGroup>
            </Project>
            """;

        var appDir = Path.Combine(_testSetup.WorkingDirectory, "src", "App");
        var libDir = Path.Combine(_testSetup.WorkingDirectory, "src", "Lib");

        TempProject.CreateFromProjectContents(appDir, "csproj", invalidProjectContents);
        TempProject.CreateFromProjectContents(libDir, "csproj", ProjectContents);

        var exitCode = Program.Main(["init", "--workingDir", _testSetup.WorkingDirectory]);

        exitCode.ShouldBe(0);
        _testPlatformAbstractions.Messages.Any(message =>
                message.Contains("contains an invalid version", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldHonorOptionCombinations()
    {
        var appDir = Path.Combine(_testSetup.WorkingDirectory, "src", "App");
        var libDir = Path.Combine(_testSetup.WorkingDirectory, "src", "Lib");

        TempProject.CreateFromProjectContents(appDir, "csproj", ProjectContents);
        TempProject.CreateFromProjectContents(libDir, "csproj", ProjectContents);

        var exitCode = Program.Main([
            "init",
            "--workingDir",
            _testSetup.WorkingDirectory,
            "--skip-project-update",
            "--initial-version",
            "2.1.0",
            "--version-element",
            "FileVersion",
            "--tag-template",
            "{name}/v{version}"]);

        exitCode.ShouldBe(0);

        var configPath = Path.Combine(_testSetup.WorkingDirectory, ".versionize");
        var config = FileConfig.Load(configPath);
        config.ShouldNotBeNull();
        config!.Projects.Length.ShouldBe(2);
        config.Projects.All(project => project.VersionElement == "FileVersion").ShouldBeTrue();
        config.Projects.All(project => project.TagTemplate == "{name}/v{version}").ShouldBeTrue();

        File.ReadAllText(Path.Combine(appDir, "App.csproj"))
            .ShouldNotContain("<FileVersion>2.1.0</FileVersion>");
        File.ReadAllText(Path.Combine(libDir, "Lib.csproj"))
            .ShouldNotContain("<FileVersion>2.1.0</FileVersion>");
    }


    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
