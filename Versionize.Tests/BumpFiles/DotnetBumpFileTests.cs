using NuGet.Versioning;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.BumpFiles;

public class DotnetBumpFileTests : IDisposable
{
    private readonly string _tempDir;

    public DotnetBumpFileTests()
    {
        _tempDir = TempDir.Create();
    }

    [Fact]
    public void ShouldDiscoverAllProjects()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"));
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"));
        TempProject.CreateVBProject(Path.Join(_tempDir, "project3"));
        TempProject.CreateProps(Path.Join(_tempDir, "project4"));
        TempProject.CreateFsharpProject(Path.Join(_tempDir, "project5"));

        // Act
        var projects = DotnetBumpFile.Create(_tempDir);

        // Assert
        projects.GetFilePaths().Count().ShouldBe(5);
    }

    [Fact]
    public void ShouldThrowForInconsistentVersions()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"), "2.0.0");
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"), "1.1.1");

        // Act/Assert
        Should.Throw<VersionizeException>(() => DotnetBumpFile.Create(_tempDir))
            .Message.ShouldBe(ErrorMessages.InconsistentProjectVersions(_tempDir, "Version"));
    }

    [Fact]
    public void ShouldWriteVersionToAllProjectFiles()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"), "1.1.1");
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"), "1.1.1");
        var projects = DotnetBumpFile.Create(_tempDir);

        // Act
        projects.WriteVersion(new SemanticVersion(2, 0, 0));

        // Assert
        var updated = DotnetBumpFile.Create(_tempDir);
        updated.Version.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }

    [Fact]
    public void ShouldDetectVersionInNamespacedXmlProjects()
    {
        // Arrange
        var projectFileContents = $"""
            <Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <PropertyGroup>
                    <Version>1.0.0</Version>
                </PropertyGroup>
            </Project>
            """;

        TempProject.CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var projects = DotnetBumpFile.Create(_tempDir);

        // Assert
        projects.Version.ShouldBe(SemanticVersion.Parse("1.0.0"));
    }

    [Fact]
    public void ShouldDiscoverFileVersionWhenVersionElementIsFileVersion()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <FileVersion>1.2.3</FileVersion>
                </PropertyGroup>
            </Project>
            """;

        TempProject.CreateFromProjectContents(Path.Join(_tempDir, "project1"), "csproj", projectFileContents);

        // Act
        var projects = DotnetBumpFile.Create(_tempDir, versionElement: "FileVersion");

        // Assert
        projects.Version.ShouldBe(SemanticVersion.Parse("1.2.3"));
    }

    [Fact]
    public void ShouldWriteFileVersionWhenVersionElementIsFileVersion()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <FileVersion>1.1.1</FileVersion>
                </PropertyGroup>
            </Project>
            """;

        TempProject.CreateFromProjectContents(Path.Join(_tempDir, "project1"), "csproj", projectFileContents);
        TempProject.CreateFromProjectContents(Path.Join(_tempDir, "project2"), "csproj", projectFileContents);

        var projects = DotnetBumpFile.Create(_tempDir, versionElement: "FileVersion");

        // Act
        projects.WriteVersion(new SemanticVersion(2, 0, 0));

        // Assert
        var updated = DotnetBumpFile.Create(_tempDir, versionElement: "FileVersion");
        updated.Version.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }

    [Fact]
    public void ShouldDiscoverAndUpdateAssemblyInfoFiles()
    {
        // Arrange
        var projectDir1 = Path.Join(_tempDir, "project1");
        var projectDir2 = Path.Join(_tempDir, "project2");

        TempProject.CreateCsharpProject(projectDir1, "1.0.0");
        TempProject.CreateCsharpProject(projectDir2, "1.0.0");

        // Create AssemblyInfo.cs files
        CreateAssemblyInfo(projectDir1, "1.0.0.0");
        CreateAssemblyInfo(projectDir2, "1.0.0.0");

        var projects = DotnetBumpFile.Create(_tempDir);

        // Act
        projects.WriteVersion(new SemanticVersion(2, 3, 4));

        // Assert
        projects.GetFilePaths().Count().ShouldBe(4); // 2 projects + 2 AssemblyInfo files

        var assemblyInfo1Content = File.ReadAllText(Path.Join(projectDir1, "Properties", "AssemblyInfo.cs"));
        assemblyInfo1Content.ShouldContain("2.3.4.0");

        var assemblyInfo2Content = File.ReadAllText(Path.Join(projectDir2, "Properties", "AssemblyInfo.cs"));
        assemblyInfo2Content.ShouldContain("2.3.4.0");
    }

    [Fact]
    public void ShouldWorkWithoutAssemblyInfoFiles()
    {
        // Arrange
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"), "1.0.0");
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"), "1.0.0");

        var projects = DotnetBumpFile.Create(_tempDir);

        // Act
        projects.WriteVersion(new SemanticVersion(2, 0, 0));

        // Assert
        projects.GetFilePaths().Count().ShouldBe(2); // Only project files, no AssemblyInfo
        var updated = DotnetBumpFile.Create(_tempDir);
        updated.Version.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }

    [Fact]
    public void ShouldUpdateAssemblyInfoWithDefaultVersionElement()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        TempProject.CreateCsharpProject(projectDir, "1.0.0");
        CreateAssemblyInfo(projectDir, "1.0.0.0");

        // Use default "Version" element for project discovery
        var projects = DotnetBumpFile.Create(_tempDir);

        // Act
        projects.WriteVersion(new SemanticVersion(3, 0, 0));

        // Assert
        var assemblyInfoContent = File.ReadAllText(Path.Join(projectDir, "Properties", "AssemblyInfo.cs"));
        assemblyInfoContent.ShouldContain("3.0.0.0");
    }

    private static void CreateAssemblyInfo(string projectDir, string version)
    {
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));
        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, $"""
            using System.Reflection;

            [assembly: AssemblyVersion("{version}")]
            [assembly: AssemblyFileVersion("{version}")]
            """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
