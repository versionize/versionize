using System;
using System.IO;
using System.Linq;
using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Tests
{
    public class ProjectsTests
    {
        [Fact]
        public void ShouldDiscoverAllProjects()
        {
            var tempDir = TempDir.Create();
            TempCsProject.Create(Path.Join(tempDir, "project1"));
            TempCsProject.Create(Path.Join(tempDir, "project2"));

            var projects = Projects.Discover(tempDir);
            projects.GetProjectFiles().Count().ShouldBe(2);
        }

        [Fact]
        public void ShouldDetectInconsistentVersions()
        {
            var tempDir = TempDir.Create();
            TempCsProject.Create(Path.Join(tempDir, "project1"), "2.0.0");
            TempCsProject.Create(Path.Join(tempDir, "project2"), "1.1.1");

            var projects = Projects.Discover(tempDir);
            projects.HasInconsistentVersioning().ShouldBeTrue();
        }

        [Fact]
        public void ShouldDetectConsistentVersions()
        {
            var tempDir = TempDir.Create();
            TempCsProject.Create(Path.Join(tempDir, "project1"));
            TempCsProject.Create(Path.Join(tempDir, "project2"));

            var projects = Projects.Discover(tempDir);
            projects.HasInconsistentVersioning().ShouldBeFalse();
        }

        [Fact]
        public void ShouldWriteAllVersionsToProjectFiles()
        {
            var tempDir = TempDir.Create();
            TempCsProject.Create(Path.Join(tempDir, "project1"), "1.1.1");
            TempCsProject.Create(Path.Join(tempDir, "project2"), "1.1.1");

            var projects = Projects.Discover(tempDir);
            projects.WriteVersion(new Version(2, 0, 0));

            var updated = Projects.Discover(tempDir);
            updated.Version.ShouldBe(Version.Parse("2.0.0"));
        }
    }
}
