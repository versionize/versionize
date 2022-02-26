using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Tests;

public class WorkingCopyTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public WorkingCopyTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldDiscoverGitWorkingCopies()
    {
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

        workingCopy.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldExitIfNoWorkingCopyCouldBeDiscovered()
    {
        var directoryWithoutWorkingCopy =
            Path.Combine(Path.GetTempPath(), "ShouldExitIfNoWorkingCopyCouldBeDiscovered");
        Directory.CreateDirectory(directoryWithoutWorkingCopy);

        Should.Throw<CommandLineExitException>(() => WorkingCopy.Discover(directoryWithoutWorkingCopy));
    }

    [Fact]
    public void ShouldExitIfWorkingCopyDoesNotExist()
    {
        var directoryWithoutWorkingCopy = Path.Combine(Path.GetTempPath(), "ShouldExitIfWorkingCopyDoesNotExist");

        Should.Throw<CommandLineExitException>(() => WorkingCopy.Discover(directoryWithoutWorkingCopy));
    }

    [Fact]
    public void ShouldPerformADryRun()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
        CommitAll(_testSetup.Repository, "feat: first commit");

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(new VersionizeOptions { DryRun = true, SkipDirty = true });

        _testPlatformAbstractions.Messages.Count.ShouldBe(7);
        _testPlatformAbstractions.Messages[0].ShouldBe("Discovered 1 versionable projects");
        _testPlatformAbstractions.Messages[3].ShouldBe("\n---");
        _testPlatformAbstractions.Messages[4].ShouldContain("* first commit");
        _testPlatformAbstractions.Messages[5].ShouldBe("---\n");
        var wasChangelogWritten = File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md"));
        Assert.False(wasChangelogWritten);
    }

    [Fact]
    public void ShouldExitIfWorkingCopyIsDirty()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));

        _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
        _testPlatformAbstractions.Messages[0].ShouldBe($"Repository {_testSetup.WorkingDirectory} is dirty. Please commit your changes.");
    }

    [Fact]
    public void ShouldExitGracefullyIfNoGitInitialized()
    {
        var workingDirectory = TempDir.Create();
        Should.Throw<CommandLineExitException>(() => WorkingCopy.Discover(workingDirectory));

        _testPlatformAbstractions.Messages[0].ShouldBe($"Directory {workingDirectory} or any parent directory do not contain a git working copy");

        Cleanup.DeleteDirectory(workingDirectory);
    }

    [Fact]
    public void ShouldExitIfWorkingCopyContainsNoProjects()
    {
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));

        _testPlatformAbstractions.Messages[0].ShouldBe($"Could not find any projects files in {_testSetup.WorkingDirectory} that have a <Version> defined in their csproj file.");
    }

    [Fact]
    public void ShouldExitIfProjectsUseInconsistentNaming()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project2"), "2.0.0");

        CommitAll(_testSetup.Repository);

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));
        _testPlatformAbstractions.Messages[0].ShouldBe($"Some projects in {_testSetup.WorkingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned");
    }

    [Fact]
    public void ShouldReleaseAsSpecifiedVersion()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");

        CommitAll(_testSetup.Repository);

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(new VersionizeOptions { ReleaseAs = "2.0.0" });

        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(new[] { "v2.0.0" });
    }

    [Fact]
    public void ShouldExitIfReleaseAsSpecifiedVersionIsInvalid()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");

        CommitAll(_testSetup.Repository);

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions { ReleaseAs = "kanguru" }));
    }

    [Fact]
    public void ShouldIgnoreInsignificantCommits()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        CommitAll(_testSetup.Repository);

        // Run versionize
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(new VersionizeOptions());

        // Add insignificant change
        File.AppendAllText(workingFilePath, "This is another line of text");
        CommitAll(_testSetup.Repository, "chore: Added line of text");

        // Get last commit
        var lastCommit = _testSetup.Repository.Head.Tip;

        // Run versionize, ignoring insignificant commits
        try
        {
            workingCopy.Versionize(new VersionizeOptions { IgnoreInsignificantCommits = true });

            throw new InvalidOperationException("Expected to throw in Versionize call");
        }
        catch (CommandLineExitException ex)
        {
            ex.ExitCode.ShouldBe(0);
        }

        lastCommit.ShouldBe(_testSetup.Repository.Head.Tip);
    }

    [Fact]
    public void ShouldExitWithNonZeroExitCodeForInsignificantCommits()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version first
        fileCommitter.CommitChange("chore: initial commit");
        workingCopy.Versionize(new VersionizeOptions());

        // Insignificant change release
        fileCommitter.CommitChange("chore: insignificant change");
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions { ExitInsignificantCommits = true }));

        _testPlatformAbstractions.Messages.Last().ShouldStartWith("Version was not affected by commits since last release");
    }

    [Fact]
    public void ShouldAddSuffixToReleaseCommitMessage()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        CommitAll(_testSetup.Repository);

        // Run versionize
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        const string suffix = "[skip ci]";
        workingCopy.Versionize(new VersionizeOptions { CommitSuffix = suffix });

        // Get last commit
        var lastCommit = _testSetup.Repository.Head.Tip;

        lastCommit.Message.ShouldContain(suffix);
    }

    [Fact]
    public void ShouldWarnAboutMissingGitConfiguration()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);

        var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

        // Create and commit a test file
        File.WriteAllText(workingFilePath, "First line of text");
        CommitAll(_testSetup.Repository);

        var configurationValues = new[] { "user.name", "user.email" }
            .SelectMany(key => Enum.GetValues(typeof(ConfigurationLevel))
            .Cast<ConfigurationLevel>()
            .Select(level => _testSetup.Repository.Config.Get<string>(key, level)))
            .Where(c => c != null)
            .ToList();

        try
        {
            configurationValues.ForEach(c => _testSetup.Repository.Config.Unset(c.Key, c.Level));

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));
            _testPlatformAbstractions.Messages.Last().ShouldStartWith("Warning: Git configuration is missing");
        }
        finally
        {
            configurationValues.ForEach(c => _testSetup.Repository.Config.Set(c.Key, c.Value, c.Level));
        }
    }

    [Fact]
    public void ShouldPrereleaseToCurrentMaximumPrereleaseVersion()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        workingCopy.Versionize(new VersionizeOptions());

        // Prerelease as minor alpha
        fileCommitter.CommitChange("feat: feature pre-release");
        workingCopy.Versionize(new VersionizeOptions { Prerelease = "alpha" });

        // Prerelease as major alpha
        fileCommitter.CommitChange("chore: initial commit\n\nBREAKING CHANGE: This is a breaking change");
        workingCopy.Versionize(new VersionizeOptions { Prerelease = "alpha" });

        var versionTagNames = VersionTagNames.ToList();
        versionTagNames.ShouldBe(new[] { "v1.0.0", "v1.1.0-alpha.0", "v2.0.0-alpha.0" });
    }

    [Fact]
    public void ShouldExitForInvalidPrereleaseSequences()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        workingCopy.Versionize(new VersionizeOptions());

        // Prerelease a minor beta
        fileCommitter.CommitChange("feat: feature pre-release");
        workingCopy.Versionize(new VersionizeOptions { Prerelease = "beta" });

        // Try Prerelease a minor alpha
        fileCommitter.CommitChange("feat: feature pre-release");
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions { Prerelease = "alpha" }));
    }

    [Fact]
    public void ShouldExitForInvalidReleaseAsReleases()
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        workingCopy.Versionize(new VersionizeOptions());

        // Release as lower than current version
        fileCommitter.CommitChange("feat: some feature");
        Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions { ReleaseAs = "0.9.0" }));
    }

    [Fact]
    public void ShouldSupportFsharpProjects()
    {
        TempProject.CreateFsharpProject(_testSetup.WorkingDirectory);

        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);

        var fileCommitter = new FileCommitter(_testSetup);

        // Release an initial version
        fileCommitter.CommitChange("chore: initial commit");
        workingCopy.Versionize(new VersionizeOptions());

        var versionTagNames = VersionTagNames.ToList();
        versionTagNames.ShouldBe(new[] { "v1.0.0" });
    }

    private IEnumerable<string> VersionTagNames
    {
        get { return _testSetup.Repository.Tags.Select(t => t.FriendlyName); }
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    private static void CommitAll(IRepository repository, string message = "feat: Initial commit")
    {
        var author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
        Commands.Stage(repository, "*");
        repository.Commit(message, author, author);
    }

    class FileCommitter
    {
        private readonly TestSetup _testSetup;

        public FileCommitter(TestSetup testSetup)
        {
            _testSetup = testSetup;
        }

        public void CommitChange(string commitMessage)
        {
            var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");
            File.WriteAllText(workingFilePath, Guid.NewGuid().ToString());
            CommitAll(_testSetup.Repository, commitMessage);
        }
    }
}
