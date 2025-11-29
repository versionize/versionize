using Shouldly;
using Versionize.CommandLine;
using Versionize.Git;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize;

public partial class RepositoryProviderTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public RepositoryProviderTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldDiscoverGitWorkingCopies()
    {
        // Arrange
        var sut = new RepositoryProvider();

        // Act
        var repository = sut.GetRepository(_testSetup.WorkingDirectory);

        repository.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldExitIfWorkingDirectoryIsNotInGitDirectory()
    {
        // Arrange
        var directoryWithoutWorkingCopy =
            Path.Combine(Path.GetTempPath(), "ShouldExitIfWorkingDirectoryIsNotInGitDirectory");
        Directory.CreateDirectory(directoryWithoutWorkingCopy);

        var sut = new RepositoryProvider();

        // Act/Assert
        Should.Throw<VersionizeException>(() => sut.GetRepository(directoryWithoutWorkingCopy))
            .Message.ShouldBe(ErrorMessages.RepositoryNotGit(directoryWithoutWorkingCopy));
    }

    [Fact]
    public void ShouldExitIfDirectoryDoesNotExist()
    {
        // Arrange
        var directoryWithoutWorkingCopy = Path.Combine(Path.GetTempPath(), "ShouldExitIfDirectoryDoesNotExist");

        var sut = new RepositoryProvider();

        // Act/Assert
        Should.Throw<VersionizeException>(() => sut.GetRepository(directoryWithoutWorkingCopy))
            .Message.ShouldBe(ErrorMessages.RepositoryDoesNotExist(directoryWithoutWorkingCopy));
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}