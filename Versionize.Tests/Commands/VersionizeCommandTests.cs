using NSubstitute;
using NuGet.Versioning;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Git;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Commands;

public class VersionizeCommandTests : IDisposable
{
    private readonly TestSetup _testSetup;

    public VersionizeCommandTests()
    {
        _testSetup = TestSetup.Create();
        CommandLineUI.Platform = new TestPlatformAbstractions();
    }

    [Fact]
    public void ShouldMonoRepoSupported()
    {
        var fileCommitter = new FileCommitter(_testSetup);

        var projectsOptions = new[]
        {
            new VersionizeOptions
            {
                WorkingDirectory = Path.Combine(_testSetup.WorkingDirectory, "project0"),
                AggregatePrereleases = true,
                Project = new ProjectOptions
                {
                    Name = "Project0",
                    Path = "project0",
                    TagTemplate = "v{version}",
                    Changelog = ChangelogOptions.Default with
                    {
                        Header = "Project0 header"
                    }
                }
            },
            new VersionizeOptions
            {
                WorkingDirectory = Path.Combine(_testSetup.WorkingDirectory, "project0-subfolder"),
                AggregatePrereleases = true,
                Project = new ProjectOptions
                {
                    Name = "Project0-subfolder",
                    Path = "project0-subfolder",
                    Changelog = ChangelogOptions.Default with
                    {
                        Header = "Project0-subfolder header",
                        Path = "docs"
                    }
                }
            },
            new VersionizeOptions
            {
                WorkingDirectory = Path.Combine(_testSetup.WorkingDirectory, "project1"),
                AggregatePrereleases = true,
                Project = new ProjectOptions
                {
                    Name = "Project1",
                    Path = "project1",
                    TagTemplate = "{name}/v{version}",
                    Changelog = ChangelogOptions.Default
                }
            },
            new VersionizeOptions
            {
                WorkingDirectory = Path.Combine(_testSetup.WorkingDirectory, "project1-legacy"),
                AggregatePrereleases = true,
                Project = new ProjectOptions
                {
                    Name = "Project1-legacy",
                    Path = "project1-legacy",
                    TagTemplate = "Project1/legacy/v{version}",
                    Changelog = ChangelogOptions.Default with
                    {
                        Header = "Project1-legacy header"
                    }
                }
            },
            new VersionizeOptions
            {
                WorkingDirectory = Path.Combine(_testSetup.WorkingDirectory, "project2"),
                AggregatePrereleases = true,
                Project = new ProjectOptions
                {
                    Name = "Project2",
                    Path = "project2",
                    Changelog = ChangelogOptions.Default with
                    {
                        Header = "Project2 header"
                    }
                }
            },
            new VersionizeOptions
            {
                WorkingDirectory = Path.Combine(_testSetup.WorkingDirectory, "project3/src"),
                AggregatePrereleases = true,
                Project = new ProjectOptions
                {
                    Name = "Project3",
                    Path = "project3/src",
                    TagTemplate = "experimental/{name}/v{version}",
                    Changelog = ChangelogOptions.Default
                }
            }
        };

        foreach (var projectOptions in projectsOptions)
        {
            var project = projectOptions.Project;

            TempProject.CreateCsharpProject(
                Path.Join(_testSetup.WorkingDirectory, project.Path));

            if (!project.Changelog.Path.Equals(string.Empty))
            {
                var changelogDir = Path.GetFullPath(Path.Combine(_testSetup.WorkingDirectory, project.Path, project.Changelog.Path));
                if (!Directory.Exists(changelogDir))
                {
                    Directory.CreateDirectory(changelogDir);
                }
            }

            var optionsProvider = Substitute.For<IVersionizeOptionsProvider>();
            optionsProvider.GetOptions().Returns(projectOptions);
            var repoProvider = Substitute.For<IRepositoryProvider>();
            repoProvider.GetRepositoryAndValidate(projectOptions).Returns(_testSetup.Repository);
            var contextProvider = new VersionizeCmdContextProvider(optionsProvider, repoProvider);
            var sut = new VersionizeCommand(contextProvider);

            // Release an initial version
            fileCommitter.CommitChange($"feat: initial commit at {project.Name}", project.Path);
            sut.OnExecute();

            // Prerelease as patch alpha
            var prereleaseOptions = projectOptions with { Prerelease = "alpha" };
            fileCommitter.CommitChange($"fix: a fix at {project.Name}", project.Path);
            optionsProvider.GetOptions().Returns(prereleaseOptions);
            sut.OnExecute();

            // Prerelease as minor alpha
            fileCommitter.CommitChange($"feat: a feature at {project.Name}", project.Path);
            optionsProvider.GetOptions().Returns(prereleaseOptions);
            sut.OnExecute();

            // Full release
            optionsProvider.GetOptions().Returns(projectOptions);
            sut.OnExecute();

            // Full release
            fileCommitter.CommitChange($"feat: another feature at {project.Name}", project.Path);
            optionsProvider.GetOptions().Returns(projectOptions);
            sut.OnExecute();
        }

        foreach (var projectOptions in projectsOptions)
        {
            var project = projectOptions.Project;

            var versionTagNames = _testSetup.Repository.Tags.Select(t => t.FriendlyName);
            var expectedTagNames = new[] { "1.0.0", "1.0.1-alpha.0", "1.1.0", "1.1.0-alpha.0", "1.2.0" }
                .Select(SemanticVersion.Parse)
                .Select(project.GetTagName);

            foreach (var expectedTag in expectedTagNames)
            {
                versionTagNames.ShouldContain(expectedTag);
            }

            var commitDate = DateTime.Now.ToString("yyyy-MM-dd");
            var changelogContents = File.ReadAllText(
                Path.GetFullPath(Path.Combine(_testSetup.WorkingDirectory, project.Path, project.Changelog.Path, "CHANGELOG.md"))
            );
            var sb = new ChangelogStringBuilder();
            sb.Append(project.Changelog.Header);

            sb.Append("<a name=\"1.2.0\"></a>");
            sb.Append($"## 1.2.0 ({commitDate})", 2);
            sb.Append("### Features", 2);
            sb.Append($"* another feature at {project.Name}", 2);

            sb.Append("<a name=\"1.1.0\"></a>");
            sb.Append($"## 1.1.0 ({commitDate})", 2);
            sb.Append("### Features", 2);
            sb.Append($"* a feature at {project.Name}", 2);
            sb.Append("### Bug Fixes", 2);
            sb.Append($"* a fix at {project.Name}", 2);

            sb.Append("<a name=\"1.1.0-alpha.0\"></a>");
            sb.Append($"## 1.1.0-alpha.0 ({commitDate})", 2);
            sb.Append("### Features", 2);
            sb.Append($"* a feature at {project.Name}", 2);
            sb.Append("### Bug Fixes", 2);
            sb.Append($"* a fix at {project.Name}", 2);

            sb.Append("<a name=\"1.0.1-alpha.0\"></a>");
            sb.Append($"## 1.0.1-alpha.0 ({commitDate})", 2);
            sb.Append("### Bug Fixes", 2);
            sb.Append($"* a fix at {project.Name}", 2);

            sb.Append("<a name=\"1.0.0\"></a>");
            sb.Append($"## 1.0.0 ({commitDate})", 2);
            sb.Append("### Features", 2);
            sb.Append($"* initial commit at {project.Name}", 2);

            var expected = sb.Build();

            Assert.Equal(expected, changelogContents);
        }
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
