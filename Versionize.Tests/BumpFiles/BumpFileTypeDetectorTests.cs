using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.BumpFiles;

public class BumpFileTypeDetectorTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public BumpFileTypeDetectorTests()
    {
        _testSetup = TestSetup.Create();
        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Theory]
    [InlineData(true, BumpFileType.None)]
    [InlineData(false, BumpFileType.Dotnet)]
    public void ReturnsDotnetProjectWhenTagOnlyIsFalse(bool tagOnly, BumpFileType expected)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var bumpFileType = BumpFileTypeDetector.GetType(_testSetup.WorkingDirectory, tagOnly);

        bumpFileType.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, BumpFileType.None)]
    [InlineData(false, BumpFileType.Unity)]
    public void ReturnsUnityProjectWhenTagOnlyIsFalse(bool tagOnly, BumpFileType expected)
    {
        TempProject.CreateUnityProject(_testSetup.WorkingDirectory);
        var bumpFileType = BumpFileTypeDetector.GetType(_testSetup.WorkingDirectory, tagOnly);

        bumpFileType.ShouldBe(expected);
    }

    [Fact]
    public void ReturnsUnityProjectWhenMonoRepo()
    {
        var projects = new[]
        {
            new ProjectOptions
            {
                Name = "Project1",
                Path = "project1",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project1 header",
                }
            },
            new ProjectOptions
            {
                Name = "Project2",
                Path = "project2",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project2 header",
                }
            }
        };

        var unityProjectPath = Path.Combine(_testSetup.WorkingDirectory, "project2");
        TempProject.CreateUnityProject(unityProjectPath);
        var bumpFileType = BumpFileTypeDetector.GetType(unityProjectPath, false);

        bumpFileType.ShouldBe(BumpFileType.Unity);
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
