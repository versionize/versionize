using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;
using Version = NuGet.Versioning.SemanticVersion;

using static Versionize.Tests.TestSupport.TempProject;

namespace Versionize.BumpFiles;

public class DotnetBumpFileProjectTests : IDisposable
{
    private readonly string _tempDir;

    public DotnetBumpFileProjectTests()
    {
        _tempDir = TempDir.Create();
    }

    // Create tests
    [Fact]
    public void ShouldThrowInCaseOfInvalidVersion()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>abcd</Version>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act/Assert
        Should.Throw<VersionizeException>(() => DotnetBumpFileProject.Create(projectFilePath));
    }

    [Fact]
    public void ShouldThrowInCaseOfInvalidXml()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>1.0.0</Version>
                </PropertyGroup>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act/Assert
        Should.Throw<VersionizeException>(() => DotnetBumpFileProject.Create(projectFilePath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldAssignVersion_When_VersionElementParamIsNullOrEmpty(string versionElement)
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>2.3.4</Version>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var project = DotnetBumpFileProject.Create(projectFilePath, versionElement);

        // Assert
        project.Version.ShouldBe(new Version(2, 3, 4));
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("FileVersion")]
    [InlineData("CustomVersion")]
    public void ShouldAssignVersion_When_VersionElementParamIsNotNullOrEmpty(string versionElement)
    {
        // Arrange
        var projectFileContents = $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <{versionElement}>2.3.4</{versionElement}>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var project = DotnetBumpFileProject.Create(projectFilePath, versionElement);

        // Assert
        project.Version.ShouldBe(new Version(2, 3, 4));
    }

    // WriteVersion tests
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdatesVersionElement_When_VersionElementParamIsNullOrEmpty(string versionElement)
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>1.0.0</Version>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);
        var project = DotnetBumpFileProject.Create(projectFilePath, versionElement);

        // Act
        project.WriteVersion(new Version(2, 0, 0));

        // Assert
        var versionedProjectContents = File.ReadAllText(projectFilePath);
        versionedProjectContents.ShouldBe(projectFileContents.Replace("1.0.0", "2.0.0"));
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("FileVersion")]
    [InlineData("CustomVersion")]
    public void UpdatesVersionElement_When_VersionElementParamIsNotNullOrEmpty(string versionElement)
    {
        // Arrange
        var projectFileContents = $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <{versionElement}>1.0.0</{versionElement}>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);
        var project = DotnetBumpFileProject.Create(projectFilePath, versionElement);

        // Act
        project.WriteVersion(new Version(2, 0, 0));

        // Assert
        var versionedProjectContents = File.ReadAllText(projectFilePath);
        versionedProjectContents.ShouldBe(projectFileContents.Replace("1.0.0", "2.0.0"));
    }

    // IsVersionable tests
    [Fact]
    public void ShouldNotBeVersionable_When_NoVersionIsContainedInProjectFile()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath);

        // Assert
        isVersionable.ShouldBeFalse();
    }

    [Fact]
    public void ShouldBeDetectedAsNotVersionable_When_AnEmptyVersionIsContainedInProjectFile()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version></Version>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath);

        // Assert
        isVersionable.ShouldBeFalse();
    }

    [Fact]
    public void ShouldNotBeVersionable_When_VersionElementIsSetToFileVersionButItsMissing()
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>1.0.0</Version>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath, versionElement: "FileVersion");

        // Assert
        isVersionable.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldBeVersionable_When_VersionElementParamIsNullOrEmpty(string versionElement)
    {
        // Arrange
        var projectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>1.0.0</Version>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath, versionElement);

        // Assert
        isVersionable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("FileVersion")]
    [InlineData("CustomVersion")]
    public void ShouldBeVersionable_When_VersionElementParamIsNotNullOrEmpty(string versionElement)
    {
        // Arrange
        var projectFileContents = $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <{versionElement}>1.0.0</{versionElement}>
                </PropertyGroup>
            </Project>
            """;

        var projectFilePath = CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        // Act
        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath, versionElement);

        // Assert
        isVersionable.ShouldBeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
