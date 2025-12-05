using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using Shouldly;
using Versionize.BumpFiles;

namespace Versionize.Lifecycle;

public class BumpFileProviderTests : IDisposable
{
    private readonly TestSetup _testSetup;

    public BumpFileProviderTests()
    {
        _testSetup = TestSetup.Create();
        CommandLineUI.Platform = new TestPlatformAbstractions();
    }

    [Fact]
    public void ReturnsUnityBumpFile_When_UnityProject()
    {
        // Arrange
        TempProject.CreateUnityProject(_testSetup.WorkingDirectory);
        var options = new IBumpFileProvider.Options
        {
            SkipBumpFile = false,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var sut = new BumpFileProvider();

        // Act
        IBumpFile bumpFile = sut.GetBumpFile(options);

        // Assert
        bumpFile.ShouldBeOfType<UnityBumpFile>();
    }

    [Fact]
    public void ReturnsNullBumpFile_When_UnityProjectAndSkipBumpFileIsTrue()
    {
        // Arrange
        TempProject.CreateUnityProject(_testSetup.WorkingDirectory);
        var options = new IBumpFileProvider.Options
        {
            SkipBumpFile = true,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var sut = new BumpFileProvider();

        // Act
        IBumpFile bumpFile = sut.GetBumpFile(options);

        // Assert
        bumpFile.ShouldBeNull();
    }

    [Fact]
    public void ReturnsDotnetBumpFile_When_DotnetProject()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var options = new IBumpFileProvider.Options
        {
            SkipBumpFile = false,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var sut = new BumpFileProvider();

        // Act
        IBumpFile bumpFile = sut.GetBumpFile(options);

        // Assert
        bumpFile.ShouldBeOfType<DotnetBumpFile>();
    }

    [Fact]
    public void ReturnsNullBumpFile_When_DotnetProjectAndSkipBumpFileIsTrue()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var options = new IBumpFileProvider.Options
        {
            SkipBumpFile = true,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var sut = new BumpFileProvider();

        // Act
        IBumpFile bumpFile = sut.GetBumpFile(options);

        // Assert
        bumpFile.ShouldBeNull();
    }

    [Fact]
    public void ReturnsNullBumpFile_When_SupportedBumpFileNotFound()
    {
        // Arrange
        var options = new IBumpFileProvider.Options
        {
            SkipBumpFile = false,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var sut = new BumpFileProvider();

        // Act
        IBumpFile bumpFile = sut.GetBumpFile(options);

        // Assert
        bumpFile.ShouldBeNull();
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
