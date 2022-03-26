using NuGet.Versioning;
using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Tests;

public class ProjectsTests
{
    [Fact]
    public void ShouldDiscoverAllProjects()
    {
        var tempDir = TempDir.Create();
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project1"));
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project2"));
        TempProject.CreateVBProject(Path.Join(tempDir, "project3"));

        var projects = Projects.Discover(tempDir);
        projects.Versionables.Count().ShouldBe(2);
    }

    [Fact]
    public void ShouldDetectInconsistentVersions()
    {
        var tempDir = TempDir.Create();
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project1"), "2.0.0");
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project2"), "1.1.1");

        var projects = Projects.Discover(tempDir);
        projects.HasInconsistentVersioning().ShouldBeTrue();
    }

    [Fact]
    public void ShouldDetectConsistentVersions()
    {
        var tempDir = TempDir.Create();
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project1"));
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project2"));

        var projects = Projects.Discover(tempDir);
        projects.HasInconsistentVersioning().ShouldBeFalse();
    }

    [Fact]
    public void ShouldWriteAllVersionsToProjectFiles()
    {
        var tempDir = TempDir.Create();
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project1"), "1.1.1");
        TempProject.CreateCsharpProject(Path.Join(tempDir, "project2"), "1.1.1");

        var projects = Projects.Discover(tempDir);
        projects.WriteVersion(new SemanticVersion(2, 0, 0));

        var updated = Projects.Discover(tempDir);
        updated.Version.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }

    [Fact]
    public void ShouldDetectVersionInNamespacedXmlProjects()
    {
        var tempDir = TempDir.Create();
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

        TempProject.CreateFromProjectContents(tempDir, "csproj", projectFileContents);

        var projects = Projects.Discover(tempDir);
        projects.Version.ShouldBe(version);
    }
}
