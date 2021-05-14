using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using Shouldly;

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
        public void ShouldPreformADryRun()
        {
            TempCsProject.Create(_testSetup.WorkingDirectory);

            File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "First commit");
            CommitAll(_testSetup.Repository);

            File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, "hello.txt"), "Second commit");
            CommitAll(_testSetup.Repository);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            workingCopy.Versionize(dryrun: true, skipDirtyCheck: true);

            _testPlatformAbstractions.Messages.Count.ShouldBe(4);
            _testPlatformAbstractions.Messages[0].ShouldBe("Discovered 1 versionable projects");
        }

        [Fact]
        public void ShouldExitIfWorkingCopyIsDirty()
        {
            TempCsProject.Create(_testSetup.WorkingDirectory);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize());

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
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize());

            _testPlatformAbstractions.Messages[0].ShouldBe($"Could not find any projects files in {_testSetup.WorkingDirectory} that have a <Version> defined in their csproj file.");
        }

        [Fact]
        public void ShouldExitIfProjectsUseInconsistentNaming()
        {
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project1"), "1.1.0");
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project2"), "2.0.0");

            CommitAll(_testSetup.Repository);

            var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize());
            _testPlatformAbstractions.Messages[0].ShouldBe($"Some projects in {_testSetup.WorkingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned");
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
            workingCopy.Versionize();

            // Add insignificant change
            File.AppendAllText(workingFilePath, "This is another line of text");
            CommitAll(_testSetup.Repository, "chore: Added line of text");

            // Get last commit
            var lastCommit = _testSetup.Repository.Head.Tip;

            // Run versionize, ignoring insignificant commits
            try
            {
                workingCopy.Versionize(ignoreInsignificant: true);

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
            workingCopy.Versionize(releaseCommitMessageSuffix: suffix);

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
