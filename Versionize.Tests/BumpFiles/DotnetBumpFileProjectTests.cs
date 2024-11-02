using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.BumpFiles;

public class DotnetBumpFileProjectTests : IDisposable
{
    private readonly string _tempDir;

    public DotnetBumpFileProjectTests()
    {
        _tempDir = TempDir.Create();
    }

    [Fact]
    public void ShouldThrowInCaseOfInvalidVersion()
    {
        var projectFileContents = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>abcd</Version>
    </PropertyGroup>
</Project>";

        var projectFilePath = Path.Join(_tempDir, "test.csproj");
        File.WriteAllText(projectFilePath, projectFileContents);

        Should.Throw<InvalidOperationException>(() => DotnetBumpFileProject.Create(projectFilePath));
    }

    [Fact]
    public void ShouldThrowInCaseOfInvalidXml()
    {
        var projectFileContents = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>1.0.0</Version>
    </PropertyGroup>
";

        var projectFilePath = Path.Join(_tempDir, "test.csproj");
        File.WriteAllText(projectFilePath, projectFileContents);

        Should.Throw<InvalidOperationException>(() => DotnetBumpFileProject.Create(projectFilePath));
    }

    [Fact]
    public void ShouldUpdateTheVersionElementOnly()
    {
        var projectFileContents =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>1.0.0</Version>
    </PropertyGroup>
</Project>";
        var projectFilePath = WriteProjectFile(_tempDir, projectFileContents);

        var project = DotnetBumpFileProject.Create(projectFilePath);
        project.WriteVersion(new Version(2, 0, 0));

        var versionedProjectContents = File.ReadAllText(projectFilePath);

        versionedProjectContents.ShouldBe(projectFileContents.Replace("1.0.0", "2.0.0"));
    }

    [Fact]
    public void ShouldNotBeVersionableIfNoVersionIsContainedInProjectFile()
    {
        var projectFilePath = WriteProjectFile(_tempDir,
@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
    </PropertyGroup>
</Project>");

        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath);
        isVersionable.ShouldBeFalse();
    }

    [Fact]
    public void ShouldBeDetectedAsNotVersionableIfAnEmptyVersionIsContainedInProjectFile()
    {
        var projectFilePath = WriteProjectFile(_tempDir,
@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version></Version>
    </PropertyGroup>
</Project>");

        var isVersionable = DotnetBumpFileProject.IsVersionable(projectFilePath);
        isVersionable.ShouldBeFalse();
    }

    private static string WriteProjectFile(string dir, string projectFileContents)
    {
        var projectFilePath = Path.Join(dir, "test.csproj");
        File.WriteAllText(projectFilePath, projectFileContents);

        return projectFilePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
