using Shouldly;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.BumpFiles;

public class BumpFileTypeDetectorTests : IDisposable
{
    private readonly TestSetup _testSetup;

    public BumpFileTypeDetectorTests()
    {
        _testSetup = TestSetup.Create();
        CommandLineUI.Platform = new TestPlatformAbstractions();
    }

    [Theory]
    [InlineData(true, BumpFileType.None)]
    [InlineData(false, BumpFileType.Dotnet)]
    public void ReturnsDotnetProjectWhenTagOnlyIsFalse(bool tagOnly, BumpFileType expected)
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        // Act
        var bumpFileType = BumpFileTypeDetector.GetType(_testSetup.WorkingDirectory, tagOnly);

        // Assert
        bumpFileType.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, BumpFileType.None)]
    [InlineData(false, BumpFileType.Unity)]
    // UnityProjectAndTagOnlyFalse_ReturnsUnityProject
    public void ReturnsUnityProjectWhenTagOnlyIsFalse(bool tagOnly, BumpFileType expected)
    {
        // Arrange
        TempProject.CreateUnityProject(_testSetup.WorkingDirectory);

        // Act
        var bumpFileType = BumpFileTypeDetector.GetType(_testSetup.WorkingDirectory, tagOnly);

        // Assert
        bumpFileType.ShouldBe(expected);
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
