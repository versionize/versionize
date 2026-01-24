using NuGet.Versioning;
using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.BumpFiles;

public class AssemblyInfoBumpFileTests : IDisposable
{
    private readonly string _tempDir;

    public AssemblyInfoBumpFileTests()
    {
        _tempDir = TempDir.Create();
    }

    [Fact]
    public void ShouldReturnNull_When_AssemblyInfoDoesNotExist()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);

        // Act
        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Assert
        assemblyInfo.ShouldBeNull();
    }

    [Fact]
    public void ShouldReturnNull_When_AssemblyInfoExistsButNoVersionAttribute()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyTitle("TestProject")]
            [assembly: AssemblyDescription("Test Description")]
            """);

        // Act
        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Assert
        assemblyInfo.ShouldBeNull();
    }

    [Fact]
    public void ShouldCreate_When_AssemblyVersionExists()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.2.3.0")]
            """);

        // Act
        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyVersion");

        // Assert
        assemblyInfo.ShouldNotBeNull();
        assemblyInfo!.FilePath.ShouldBe(assemblyInfoPath);
    }

    [Fact]
    public void ShouldCreate_When_AssemblyFileVersionExists()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyFileVersion("1.2.3.0")]
            """);

        // Act
        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyFileVersion");

        // Assert
        assemblyInfo.ShouldNotBeNull();
        assemblyInfo!.FilePath.ShouldBe(assemblyInfoPath);
    }

    [Fact]
    public void ShouldCreate_When_VersionElementIsVersionAndEitherAttributeExists()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.2.3.0")]
            """);

        // Act
        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Assert
        assemblyInfo.ShouldNotBeNull();
        assemblyInfo!.FilePath.ShouldBe(assemblyInfoPath);
    }

    [Fact]
    public void ShouldUpdateAssemblyVersion_When_VersionElementIsAssemblyVersion()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyVersion");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(2, 3, 4));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyVersion(\"2.3.4.0\")]");
        content.ShouldContain("[assembly: AssemblyFileVersion(\"1.0.0.0\")]"); // Should not change
    }

    [Fact]
    public void ShouldUpdateAssemblyFileVersion_When_VersionElementIsAssemblyFileVersion()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyFileVersion");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(2, 3, 4));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyVersion(\"1.0.0.0\")]"); // Should not change
        content.ShouldContain("[assembly: AssemblyFileVersion(\"2.3.4.0\")]");
    }

    [Fact]
    public void ShouldUpdateBothVersions_When_VersionElementIsVersion()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(2, 3, 4));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyVersion(\"2.3.4.0\")]");
        content.ShouldContain("[assembly: AssemblyFileVersion(\"2.3.4.0\")]");
    }

    [Fact]
    public void ShouldHandleVariousWhitespaceFormats()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly:AssemblyVersion("1.0.0.0")]
            [assembly:  AssemblyFileVersion(  "1.0.0.0"  )]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(3, 2, 1));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyVersion(\"3.2.1.0\")]");
        content.ShouldContain("[assembly: AssemblyFileVersion(\"3.2.1.0\")]");
    }

    [Fact]
    public void ShouldPreserveOtherAttributes()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        var originalContent = """
            using System.Reflection;
            using System.Runtime.InteropServices;

            [assembly: AssemblyTitle("TestProject")]
            [assembly: AssemblyDescription("A test project")]
            [assembly: AssemblyConfiguration("")]
            [assembly: AssemblyCompany("Test Company")]
            [assembly: AssemblyProduct("TestProduct")]
            [assembly: AssemblyCopyright("Copyright © 2024")]
            [assembly: AssemblyTrademark("")]
            [assembly: AssemblyCulture("")]
            [assembly: ComVisible(false)]
            [assembly: Guid("12345678-1234-1234-1234-123456789012")]
            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """;
        File.WriteAllText(assemblyInfoPath, originalContent);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(5, 6, 7));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyTitle(\"TestProject\")]");
        content.ShouldContain("[assembly: AssemblyDescription(\"A test project\")]");
        content.ShouldContain("[assembly: AssemblyCompany(\"Test Company\")]");
        content.ShouldContain("[assembly: AssemblyVersion(\"5.6.7.0\")]");
        content.ShouldContain("[assembly: AssemblyFileVersion(\"5.6.7.0\")]");
    }

    [Fact]
    public void ShouldHandleCustomVersionElement()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyInformationalVersion("1.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyInformationalVersion");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(4, 5, 6));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyInformationalVersion(\"4.5.6.0\")]");
    }

    [Fact]
    public void ShouldFormatVersionAsExpected()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyVersion");

        // Act
        assemblyInfo!.WriteVersion(new SemanticVersion(10, 20, 30));

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyVersion(\"10.20.30.0\")]");
    }

    [Fact]
    public void ShouldDiscardPrereleaseLabel_When_UpdatingAssemblyVersion()
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, "project1");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "Version");

        // Act - Write a pre-release version
        var prereleaseVersion = SemanticVersion.Parse("2.3.4-alpha.1");
        assemblyInfo!.WriteVersion(prereleaseVersion);

        // Assert - Pre-release label should be discarded, only numeric version written
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain("[assembly: AssemblyVersion(\"2.3.4.0\")]");
        content.ShouldContain("[assembly: AssemblyFileVersion(\"2.3.4.0\")]");
        content.ShouldNotContain("alpha");
        content.ShouldNotContain("2.3.4-alpha.1");
    }

    [Theory]
    [InlineData("1.2.3-beta.2", "1.2.3.0")]
    [InlineData("3.0.0-rc.1", "3.0.0.0")]
    [InlineData("2.5.1-alpha", "2.5.1.0")]
    [InlineData("1.0.0", "1.0.0.0")]
    public void ShouldConvertPrereleaseVersionsToNumericFormat(string semanticVersion, string expectedAssemblyVersion)
    {
        // Arrange
        var projectDir = Path.Join(_tempDir, $"project_{semanticVersion.Replace(".", "_").Replace("-", "_")}");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Join(projectDir, "Properties"));

        var assemblyInfoPath = Path.Join(projectDir, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath, """
            using System.Reflection;

            [assembly: AssemblyVersion("0.0.0.0")]
            """);

        var assemblyInfo = AssemblyInfoBumpFile.TryCreate(projectDir, "AssemblyVersion");

        // Act
        var version = SemanticVersion.Parse(semanticVersion);
        assemblyInfo!.WriteVersion(version);

        // Assert
        var content = File.ReadAllText(assemblyInfoPath);
        content.ShouldContain($"[assembly: AssemblyVersion(\"{expectedAssemblyVersion}\")]");
    }

    [Fact]
    public void ShouldUpdateOnlyExistingAttribute_When_VersionElementIsVersionButOnlyOneAttributeExists()
    {
        // Arrange - File contains only AssemblyVersion
        var projectDir1 = Path.Join(_tempDir, "project_only_assemblyversion");
        Directory.CreateDirectory(projectDir1);
        Directory.CreateDirectory(Path.Join(projectDir1, "Properties"));

        var assemblyInfoPath1 = Path.Join(projectDir1, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath1, """
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyTitle("TestProject")]
            """);

        var assemblyInfo1 = AssemblyInfoBumpFile.TryCreate(projectDir1, "Version");

        // Act
        assemblyInfo1!.WriteVersion(new SemanticVersion(2, 0, 0));

        // Assert - Only AssemblyVersion should be updated
        var content1 = File.ReadAllText(assemblyInfoPath1);
        content1.ShouldContain("[assembly: AssemblyVersion(\"2.0.0.0\")]");
        content1.ShouldNotContain("AssemblyFileVersion"); // Should not add missing attribute

        // Arrange - File contains only AssemblyFileVersion
        var projectDir2 = Path.Join(_tempDir, "project_only_fileversion");
        Directory.CreateDirectory(projectDir2);
        Directory.CreateDirectory(Path.Join(projectDir2, "Properties"));

        var assemblyInfoPath2 = Path.Join(projectDir2, "Properties", "AssemblyInfo.cs");
        File.WriteAllText(assemblyInfoPath2, """
            using System.Reflection;

            [assembly: AssemblyFileVersion("1.0.0.0")]
            [assembly: AssemblyTitle("TestProject")]
            """);

        var assemblyInfo2 = AssemblyInfoBumpFile.TryCreate(projectDir2, "Version");

        // Act
        assemblyInfo2!.WriteVersion(new SemanticVersion(3, 0, 0));

        // Assert - Only AssemblyFileVersion should be updated
        var content2 = File.ReadAllText(assemblyInfoPath2);
        content2.ShouldContain("[assembly: AssemblyFileVersion(\"3.0.0.0\")]");
        content2.ShouldNotContain("AssemblyVersion"); // Should not add missing attribute
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
