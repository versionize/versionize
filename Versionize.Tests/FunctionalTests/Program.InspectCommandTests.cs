using Newtonsoft.Json;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public class InspectCommandTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public InspectCommandTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
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
    public void ShouldExitIfNoProjectWithVersionIsFound()
    {
        // Arrange
        TempProject.CreateFromProjectContents(_testSetup.WorkingDirectory, "csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                </PropertyGroup>
            </Project>
            """);

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "inspect"]);

        // Assert
        exitCode.ShouldBe(1);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe(ErrorMessages.NoVersionableProjects(_testSetup.WorkingDirectory, "Version"));
    }

    [Fact]
    public void ShouldExitForProjectsInconsistentVersion()
    {
        // Arrange
        TempProject.CreateFromProjectContents(_testSetup.WorkingDirectory + "/project1", "csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>1.0.0</Version>
                </PropertyGroup>
            </Project>
            """);

        TempProject.CreateFromProjectContents(_testSetup.WorkingDirectory + "/project2", "csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>2.0.0</Version>
                </PropertyGroup>
            </Project>
            """);

        // Act
        var exitCode = Program.Main(["--workingDir", _testSetup.WorkingDirectory, "inspect"]);

        // Assert
        exitCode.ShouldBe(1);
        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe(ErrorMessages.InconsistentProjectVersions(_testSetup.WorkingDirectory, "Version"));
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
