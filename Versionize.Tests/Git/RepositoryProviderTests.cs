using LibGit2Sharp;
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
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        // Act
        var repository = sut.GetRepository(options);

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
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = directoryWithoutWorkingCopy,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        // Act/Assert
        Should.Throw<VersionizeException>(() => sut.GetRepository(options))
            .Message.ShouldBe(ErrorMessages.RepositoryNotGit(directoryWithoutWorkingCopy));
    }

    [Fact]
    public void ShouldExitIfDirectoryDoesNotExist()
    {
        // Arrange
        var directoryWithoutWorkingCopy = Path.Combine(Path.GetTempPath(), "ShouldExitIfDirectoryDoesNotExist");

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = directoryWithoutWorkingCopy,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        // Act/Assert
        Should.Throw<VersionizeException>(() => sut.GetRepository(options))
            .Message.ShouldBe(ErrorMessages.RepositoryDoesNotExist(directoryWithoutWorkingCopy));
    }

    [Fact]
    public void ShouldNotThrowForUntrackedFiles()
    {
        // Arrange
        // Untracked file is the csproj.
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.GetRepositoryAndValidate(options));
    }

    [Fact]
    public void ShouldExitIfWorkingCopyIsDirty()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        GitTestHelpers.CommitAll(_testSetup.Repository, "feat: first commit");

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "hello world");
        LibGit2Sharp.Commands.Stage(_testSetup.Repository, "*");

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        // Act/Assert
        Should.Throw<VersionizeException>(() => sut.GetRepositoryAndValidate(options))
            .Message.ShouldBe(ErrorMessages.RepositoryDirty(_testSetup.WorkingDirectory, "NewInIndex: hello.txt"));
    }

    [Fact]
    public void ShouldThrowForMissingGitUserConfig()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        var configurationValues = new[] { "user.name", "user.email" }
            .Select(key => _testSetup.Repository.Config.Get<string>(key, ConfigurationLevel.Local))
            .Where(c => c != null)
            .ToList();

        configurationValues.ForEach(c => _testSetup.Repository.Config.Unset(c.Key, c.Level));

        _testSetup.Repository.Config.Get<string>("user.name").ShouldBeNull();
        _testSetup.Repository.Config.Get<string>("user.email").ShouldBeNull();

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.GetRepositoryAndValidate(options))
            .Message.ShouldBe(ErrorMessages.GitConfigMissing());
    }

    [Fact]
    public void ShouldNotThrowForMissingGitConfiguration_When_SkippingCommitAndTag()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        var configurationValues = new[] { "user.name", "user.email" }
            .Select(key => _testSetup.Repository.Config.Get<string>(key, ConfigurationLevel.Local))
            .Where(c => c != null)
            .ToList();

        configurationValues.ForEach(c => _testSetup.Repository.Config.Unset(c.Key, c.Level));

        _testSetup.Repository.Config.Get<string>("user.name").ShouldBeNull();
        _testSetup.Repository.Config.Get<string>("user.email").ShouldBeNull();

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = true,
            SkipTag = true,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.GetRepositoryAndValidate(options));
    }

    [Fact]
    public void ShouldWarnAboutMissingGitConfiguration_When_OnlySkippingCommit()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        var configurationValues = new[] { "user.name", "user.email" }
            .Select(key => _testSetup.Repository.Config.Get<string>(key, ConfigurationLevel.Local))
            .Where(c => c != null)
            .ToList();

        configurationValues.ForEach(c => _testSetup.Repository.Config.Unset(c.Key, c.Level));

        _testSetup.Repository.Config.Get<string>("user.name").ShouldBeNull();
        _testSetup.Repository.Config.Get<string>("user.email").ShouldBeNull();

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = true,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.GetRepositoryAndValidate(options))
            .Message.ShouldBe(ErrorMessages.GitConfigMissing());
    }

    [Fact]
    public void ShouldWarnAboutMissingGitConfigurationWhenOnlySkippingTag()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        GitTestHelpers.CommitAll(_testSetup.Repository);

        var configurationValues = new[] { "user.name", "user.email" }
            .Select(key => _testSetup.Repository.Config.Get<string>(key, ConfigurationLevel.Local))
            .Where(c => c != null)
            .ToList();

        configurationValues.ForEach(c => _testSetup.Repository.Config.Unset(c.Key, c.Level));

        _testSetup.Repository.Config.Get<string>("user.name").ShouldBeNull();
        _testSetup.Repository.Config.Get<string>("user.email").ShouldBeNull();

        var sut = new RepositoryProvider();
        var options = new IRepositoryProvider.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = true,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.GetRepositoryAndValidate(options))
            .Message.ShouldBe(ErrorMessages.GitConfigMissing());
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}