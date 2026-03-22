using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Git;
using Versionize.Tests.TestSupport;
using Xunit;
using Xunit.Sdk;
using static LibGit2Sharp.FileStatus;

namespace Versionize;

public partial class RepoStateValidatorTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public RepoStateValidatorTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldNotThrowForUntrackedFiles()
    {
        // Arrange
        // Untracked file is the csproj.
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.Validate(_testSetup.Repository, options));
    }

    [Fact]
    public void ShouldExitIfWorkingCopyIsDirty()
    {
        // Arrange
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        GitTestHelpers.CommitAll(_testSetup.Repository, "feat: first commit");

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "hello world");
        LibGit2Sharp.Commands.Stage(_testSetup.Repository, "*");

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        // Act/Assert
        Should.Throw<VersionizeException>(() => sut.Validate(_testSetup.Repository, options))
            .Message.ShouldBe(ErrorMessages.RepositoryDirty(_testSetup.WorkingDirectory, "NewInIndex: hello.txt"));
    }

    [Theory]
    [InlineData(".claude")]
    [InlineData(".agent")]
    [InlineData(".agents")]
    [InlineData(".cursor")]
    [InlineData(".winsurf")]
    [InlineData(".windsurf")]
    [InlineData(".opencode")]
    [InlineData(".codex")]
    public void ShouldIgnoreTrackedToolSymlinkChangesInAllowedDirectories(string toolDirectory)
    {
        RequireWindows();
        ReplaceTrackedFileWithSymlinkOrSkip(Path.Join(toolDirectory, "SKILL.md"));

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.Validate(_testSetup.Repository, options));
        _testPlatformAbstractions.Messages.ShouldContain(InfoMessages.IgnoredToolSymlinks(1, RepoStateValidator.IgnoredToolDirectoryList));
        _testPlatformAbstractions.Messages.ShouldContain($"  * {NormalizeGitPath(Path.Join(toolDirectory, "SKILL.md"))}");
    }

    [Fact]
    public void ShouldIgnoreToolSymlinksButStillFailForOtherDirtyFiles()
    {
        RequireWindows();
        ReplaceTrackedFileWithSymlinkOrSkip(Path.Join(".claude", "SKILL.md"));

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "hello world");
        LibGit2Sharp.Commands.Stage(_testSetup.Repository, "hello.txt");

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        var exception = Should.Throw<VersionizeException>(() => sut.Validate(_testSetup.Repository, options));
        exception.Message.ShouldBe(ErrorMessages.RepositoryDirty(_testSetup.WorkingDirectory, "NewInIndex: hello.txt"));
        _testPlatformAbstractions.Messages.ShouldContain(InfoMessages.IgnoredToolSymlinks(1, RepoStateValidator.IgnoredToolDirectoryList));
        _testPlatformAbstractions.Messages.ShouldContain("  * .claude/SKILL.md");
    }

    [Fact]
    public void ShouldTreatSymlinkOutsideAllowedDirectoriesAsDirty()
    {
        RequireWindows();
        ReplaceTrackedFileWithSymlinkOrSkip(Path.Join(".tools", "SKILL.md"));

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.Validate(_testSetup.Repository, options))
            .Message.ShouldContain(".tools/SKILL.md");
    }

    [Fact]
    public void ShouldWarnWithCorrectCountForMultipleIgnoredToolSymlinks()
    {
        RequireWindows();
        ReplaceTrackedFileWithSymlinkOrSkip(Path.Join(".claude", "SKILL.md"));
        ReplaceTrackedFileWithSymlinkOrSkip(Path.Join(".cursor", "AGENTS.md"));

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.Validate(_testSetup.Repository, options));
        _testPlatformAbstractions.Messages.ShouldContain(InfoMessages.IgnoredToolSymlinks(2, RepoStateValidator.IgnoredToolDirectoryList));
        _testPlatformAbstractions.Messages.ShouldContain("  * .claude/SKILL.md");
        _testPlatformAbstractions.Messages.ShouldContain("  * .cursor/AGENTS.md");
    }

    [Fact]
    public void ShouldNotListIgnoredToolSymlinksWhenOutputIsSilent()
    {
        RequireWindows();
        ReplaceTrackedFileWithSymlinkOrSkip(Path.Join(".claude", "SKILL.md"));

        CommandLineUI.Verbosity = Versionize.CommandLine.LogLevel.Silent;

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.Validate(_testSetup.Repository, options));
        _testPlatformAbstractions.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldDetectRealWindowsSymlinkInTestSymLinkFolder()
    {
        RequireWindows();

        var testSymLinkDirectory = Path.Join(_testSetup.WorkingDirectory, "testSymLink");
        var claudeDirectory = Path.Join(testSymLinkDirectory, ".claude");
        var agentDirectory = Path.Join(testSymLinkDirectory, ".agent");
        Directory.CreateDirectory(claudeDirectory);
        Directory.CreateDirectory(agentDirectory);

        var realFilePath = Path.Join(claudeDirectory, "real.txt");
        var symlinkPath = Path.Join(agentDirectory, "real.txt");
        File.WriteAllText(realFilePath, "real file content");

        CreateFileSymlinkOrThrow(symlinkPath, realFilePath);

        File.Exists(symlinkPath).ShouldBeTrue();
        File.GetAttributes(symlinkPath).HasFlag(FileAttributes.ReparsePoint).ShouldBeTrue();
        RepoStateValidator.IsWindowsReparsePoint(symlinkPath).ShouldBeTrue();
        RepoStateValidator.IsWindowsReparsePoint(realFilePath).ShouldBeFalse();
    }

    [Fact]
    public void ShouldRecognizeExistingDeletedFromWorkdirEntryInToolDirectoryAsIgnorable()
    {
        RequireWindows();

        var testSymLinkDirectory = Path.Join(_testSetup.WorkingDirectory, "testSymLink");
        var agentDirectory = Path.Join(testSymLinkDirectory, ".agent");
        Directory.CreateDirectory(agentDirectory);

        var existingFilePath = Path.Join(agentDirectory, "real.txt");
        File.WriteAllText(existingFilePath, "real file content");

        var repositoryRoot = Path.GetFullPath(testSymLinkDirectory);
        var fullPath = Path.GetFullPath(existingFilePath);

        RepoStateValidator.IsToolDirectoryPath(fullPath, repositoryRoot).ShouldBeTrue();
        RepoStateValidator.PathExists(fullPath).ShouldBeTrue();
        RepoStateValidator.IsWindowsReparsePoint(fullPath).ShouldBeFalse();
        RepoStateValidator.IsIgnoredExistingToolEntry(
            ".agent/real.txt",
            DeletedFromWorkdir,
            repositoryRoot).ShouldBeTrue();
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

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.Validate(_testSetup.Repository, options))
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

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = true,
            SkipTag = true,
            SkipDirty = false,
            DryRun = false
        };

        Should.NotThrow(() => sut.Validate(_testSetup.Repository, options));
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

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = true,
            SkipTag = false,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.Validate(_testSetup.Repository, options))
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

        var sut = new RepoStateValidator();
        var options = new IRepoStateValidator.Options
        {
            WorkingDirectory = _testSetup.WorkingDirectory,
            SkipCommit = false,
            SkipTag = true,
            SkipDirty = false,
            DryRun = false
        };

        Should.Throw<VersionizeException>(() => sut.Validate(_testSetup.Repository, options))
            .Message.ShouldBe(ErrorMessages.GitConfigMissing());
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    private void ReplaceTrackedFileWithSymlinkOrSkip(string relativePath)
    {
        var fullPath = Path.Join(_testSetup.WorkingDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        directory.ShouldNotBeNull();
        Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, "tracked");
        CommitPath(relativePath, $"feat: add {relativePath}");

        var targetPath = Path.Join(_testSetup.WorkingDirectory, Path.GetFileNameWithoutExtension(relativePath) + ".target.txt");
        File.WriteAllText(targetPath, "target");

        File.Delete(fullPath);
        CreateFileSymlinkOrThrow(fullPath, targetPath);
    }

    private void CommitPath(string relativePath, string message)
    {
        LibGit2Sharp.Commands.Stage(_testSetup.Repository, NormalizeGitPath(relativePath));

        var user = _testSetup.Repository.Config.Get<string>("user.name")?.Value;
        var email = _testSetup.Repository.Config.Get<string>("user.email")?.Value;
        var signature = new Signature(user, email, DateTimeOffset.Now);

        _testSetup.Repository.Commit(message, signature, signature);
    }

    private static string NormalizeGitPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static void CreateFileSymlinkOrThrow(string symlinkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(symlinkPath, targetPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Skip(ex);
        }
        catch (IOException ex)
        {
            throw Skip(ex);
        }
        catch (PlatformNotSupportedException ex)
        {
            throw Skip(ex);
        }
    }

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw SkipException.ForSkip("Windows-only test.");
        }
    }

    private static SkipException Skip()
    {
        return SkipException.ForSkip("Windows symbolic links are not available in the current test environment.");
    }

    private static SkipException Skip(Exception ex)
    {
        return SkipException.ForSkip($"Windows symbolic links are not available in the current test environment. {ex.GetType().Name}: {ex.Message}");
    }

}
