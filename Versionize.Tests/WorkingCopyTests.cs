using LibGit2Sharp;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Tests
{
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
            TempCsProject.Create(_testSetup.WorkingDirectory);

            File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
            CommitAll(_testSetup.Repository, "feat: first commit");

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            workingCopy.Versionize(new VersionizeOptions { DryRun = true, SkipDirty = true });

            _testPlatformAbstractions.Messages.Count.ShouldBe(7);
            _testPlatformAbstractions.Messages[0].Message.ShouldBe("Discovered 1 versionable projects");
            _testPlatformAbstractions.Messages[3].Message.ShouldBe("\n---");
            _testPlatformAbstractions.Messages[4].Message.ShouldContain("* first commit");
            _testPlatformAbstractions.Messages[5].Message.ShouldBe("---\n");
            var wasChangelogWritten = File.Exists(Path.Join(_testSetup.WorkingDirectory, "CHANGELOG.md"));
            Assert.False(wasChangelogWritten);
        }

        [Fact]
        public void ShouldExitIfWorkingCopyIsDirty()
        {
            TempCsProject.Create(_testSetup.WorkingDirectory);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));

            _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
            _testPlatformAbstractions.Messages[0].Message.ShouldBe($"Repository {_testSetup.WorkingDirectory} is dirty. Please commit your changes.");
        }

        [Fact]
        public void ShouldExitGracefullyIfNoGitInitialized()
        {
            var workingDirectory = TempDir.Create();
            Should.Throw<CommandLineExitException>(() => WorkingCopy.Discover(workingDirectory));

            _testPlatformAbstractions.Messages[0].Message.ShouldBe($"Directory {workingDirectory} or any parent directory do not contain a git working copy");

            Cleanup.DeleteDirectory(workingDirectory);
        }

        [Fact]
        public void ShouldExitIfWorkingCopyContainsNoProjects()
        {
            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));

            _testPlatformAbstractions.Messages[0].Message.ShouldBe($"Could not find any projects files in {_testSetup.WorkingDirectory} that have a <Version> defined in their csproj file.");
        }

        [Fact]
        public void ShouldExitIfProjectsUseInconsistentNaming()
        {
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project2"), "2.0.0");

            CommitAll(_testSetup.Repository);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions()));
            _testPlatformAbstractions.Messages[0].Message.ShouldBe($"Some projects in {_testSetup.WorkingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned");
        }

        [Fact]
        public void ShouldReleaseAsSpecifiedVersion()
        {
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");

            CommitAll(_testSetup.Repository);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            workingCopy.Versionize(new VersionizeOptions { ReleaseAs = "2.0.0" });

            _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(new[] { "v2.0.0" });
        }

        [Fact]
        public void ShouldExitIfReleaseAsSpecifiedVersionIsInvalid()
        {
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");

            CommitAll(_testSetup.Repository);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize(new VersionizeOptions { ReleaseAs = "kanguru" }));
        }

        [Fact]
        public void ShouldIgnoreInsignificantCommits()
        {
            TempCsProject.Create(_testSetup.WorkingDirectory);

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
        public void ShouldAddSuffixToReleaseCommitMessage()
        {
            TempCsProject.Create(_testSetup.WorkingDirectory);

            var workingFilePath = Path.Join(_testSetup.WorkingDirectory, "hello.txt");

            // Create and commit a test file
            File.WriteAllText(workingFilePath, "First line of text");
            CommitAll(_testSetup.Repository);

            // Run versionize
            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            var suffix = "[skip ci]";
            workingCopy.Versionize(new VersionizeOptions { CommitSuffix = suffix });

            // Get last commit
            var lastCommit = _testSetup.Repository.Head.Tip;

            lastCommit.Message.ShouldContain(suffix);
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
    }
}
