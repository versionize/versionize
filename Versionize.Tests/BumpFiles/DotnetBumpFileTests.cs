using NuGet.Versioning;
using Shouldly;
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
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"));
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"));
        TempProject.CreateVBProject(Path.Join(_tempDir, "project3"));
        TempProject.CreateProps(Path.Join(_tempDir, "project4"));

        var projects = DotnetBumpFile.Create(_tempDir);
        projects.GetFilePaths().Count().ShouldBe(4);
    }

    //[Fact]
    //public void ShouldDetectInconsistentVersions()
    //{
    //    TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"), "2.0.0");
    //    TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"), "1.1.1");

    //    var projects = DotnetBumpFile.Discover(_tempDir);
    //    projects.HasInconsistentVersioning().ShouldBeTrue();
    //}

    //[Fact]
    //public void ShouldDetectConsistentVersions()
    //{
    //    TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"));
    //    TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"));

    //    var projects = DotnetBumpFile.Discover(_tempDir);
    //    projects.HasInconsistentVersioning().ShouldBeFalse();
    //}

    [Fact]
    public void ShouldWriteAllVersionsToProjectFiles()
    {
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project1"), "1.1.1");
        TempProject.CreateCsharpProject(Path.Join(_tempDir, "project2"), "1.1.1");

        var projects = DotnetBumpFile.Create(_tempDir);
        projects.WriteVersion(new SemanticVersion(2, 0, 0));

        var updated = DotnetBumpFile.Create(_tempDir);
        updated.Version.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }

    [Fact]
    public void ShouldDetectVersionInNamespacedXmlProjects()
    {
        var version = SemanticVersion.Parse("1.0.0");

        // Create .net project
        var projectFileContents =
$@"<Project ToolsVersion=""12.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
   <PropertyGroup>
        ...
        <Version>{version}</Version>
        ...
     </PropertyGroup>
</Project>";

        TempProject.CreateFromProjectContents(_tempDir, "csproj", projectFileContents);

        var projects = DotnetBumpFile.Create(_tempDir);
        projects.Version.ShouldBe(version);
    }

    [Fact]
    public void ShouldDiscoverFileVersionWhenVersionElementIsFileVersion()
    {
        var projectFileContents =
@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <FileVersion>1.2.3</FileVersion>
    </PropertyGroup>
</Project>";

        TempProject.CreateFromProjectContents(Path.Join(_tempDir, "project1"), "csproj", projectFileContents);

        var projects = DotnetBumpFile.Create(_tempDir, versionElement: "FileVersion");
        projects.Version.ShouldBe(SemanticVersion.Parse("1.2.3"));
    }

    [Fact]
    public void ShouldWriteFileVersionToProjectFilesWhenVersionElementIsFileVersion()
    {
        var projectFileContents =
@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <FileVersion>1.1.1</FileVersion>
    </PropertyGroup>
</Project>";

        TempProject.CreateFromProjectContents(Path.Join(_tempDir, "project1"), "csproj", projectFileContents);
        TempProject.CreateFromProjectContents(Path.Join(_tempDir, "project2"), "csproj", projectFileContents);

        var projects = DotnetBumpFile.Create(_tempDir, versionElement: "FileVersion");
        projects.WriteVersion(new SemanticVersion(2, 0, 0));

        var updated = DotnetBumpFile.Create(_tempDir, versionElement: "FileVersion");
        updated.Version.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
