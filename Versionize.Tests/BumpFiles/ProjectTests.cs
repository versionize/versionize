using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.BumpFiles;

public class ProjectTests
{
    [Fact]
    public void ShouldThrowInCaseOfInvalidVersion()
    {
        var tempDir = TempDir.Create();
        var projectFileContents = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>abcd</Version>
    </PropertyGroup>
</Project>";

        var projectFilePath = Path.Join(tempDir, "test.csproj");
        File.WriteAllText(projectFilePath, projectFileContents);

        Should.Throw<InvalidOperationException>(() => Project.Create(projectFilePath));
    }

    [Fact]
    public void ShouldThrowInCaseOfInvalidXml()
    {
        var tempDir = TempDir.Create();
        var projectFileContents = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>1.0.0</Version>
    </PropertyGroup>
";

        var projectFilePath = Path.Join(tempDir, "test.csproj");
        File.WriteAllText(projectFilePath, projectFileContents);

        Should.Throw<InvalidOperationException>(() => Project.Create(projectFilePath));
    }

    [Fact]
    public void ShouldUpdateTheVersionElementOnly()
    {
        var tempDir = TempDir.Create();
        var projectFileContents =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>1.0.0</Version>
    </PropertyGroup>
</Project>";
        var projectFilePath = WriteProjectFile(tempDir, projectFileContents);

        var project = Project.Create(projectFilePath);
        project.WriteVersion(new Version(2, 0, 0));

        var versionedProjectContents = File.ReadAllText(projectFilePath);

        versionedProjectContents.ShouldBe(projectFileContents.Replace("1.0.0", "2.0.0"));
    }

    [Fact]
    public void ShouldNotBeVersionableIfNoVersionIsContainedInProjectFile()
    {
        var tempDir = TempDir.Create();
        var projectFilePath = WriteProjectFile(tempDir,
@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
    </PropertyGroup>
</Project>");

        var isVersionable = Project.IsVersionable(projectFilePath);
        isVersionable.ShouldBeFalse();
    }

    [Fact]
    public void ShouldBeDetectedAsNotVersionableIfAnEmptyVersionIsContainedInProjectFile()
    {
        var tempDir = TempDir.Create();
        var projectFilePath = WriteProjectFile(tempDir,
@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version></Version>
    </PropertyGroup>
</Project>");

        var isVersionable = Project.IsVersionable(projectFilePath);
        isVersionable.ShouldBeFalse();
    }

    private static string WriteProjectFile(string dir, string projectFileContents)
    {
        var projectFilePath = Path.Join(dir, "test.csproj");
        File.WriteAllText(projectFilePath, projectFileContents);

        return projectFilePath;
    }
}
