using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using Shouldly;

namespace Versionize.Tests
{
    public class WorkingCopyTests
    {
        private readonly TestPlatformAbstractions _testPlatformAbstractions;

        public WorkingCopyTests()
        {
            _testPlatformAbstractions = new TestPlatformAbstractions();
            CommandLineUI.Platform = _testPlatformAbstractions;
        }

        [Fact]
        public void ShouldDiscoverGitWorkingCopies()
        {
            var workingCopy = WorkingCopy.Discover(Directory.GetCurrentDirectory());

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
            var workingCopy = WorkingCopy.Discover(Directory.GetCurrentDirectory());
            workingCopy.Versionize(dryrun: true, skipDirtyCheck: true);

            _testPlatformAbstractions.Messages.Count.ShouldBe(4);
            _testPlatformAbstractions.Messages[0].ShouldBe("Discovered 1 versionable projects");
        }

        [Fact]
        public void ShouldExitIfWorkingCopyIsDirty()
        {
            var workingDirectory = TempDir.Create();
            using var tempRepository = TempRepository.Create(workingDirectory);
            
            TempCsProject.Create(workingDirectory);

            var workingCopy = WorkingCopy.Discover(workingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize());
            
            _testPlatformAbstractions.Messages.ShouldHaveSingleItem();
            _testPlatformAbstractions.Messages[0].ShouldBe($"Repository {workingDirectory} is dirty. Please commit your changes.");

            Cleanup.DeleteDirectory(workingDirectory);
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
            var workingDirectory = TempDir.Create();
            using var tempRepository = TempRepository.Create(workingDirectory);

            var workingCopy = WorkingCopy.Discover(workingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize());
            
            _testPlatformAbstractions.Messages[0].ShouldBe($"Could not find any projects files in {workingDirectory} that have a <Version> defined in their csproj file.");
            
            Cleanup.DeleteDirectory(workingDirectory);
        }

        [Fact]
        public void ShouldExitIfProjectsUseInconsistentNaming()
        {
            var workingDirectory = TempDir.Create();
            using var tempRepository = TempRepository.Create(workingDirectory);

            TempCsProject.Create(Path.Join(workingDirectory, "project1"), "1.1.0");
            TempCsProject.Create(Path.Join(workingDirectory, "project2"), "2.0.0");

            CommitAll(tempRepository);

            var workingCopy = WorkingCopy.Discover(workingDirectory);
            Should.Throw<CommandLineExitException>(() => workingCopy.Versionize());
            _testPlatformAbstractions.Messages[0].ShouldBe($"Some projects in {workingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned");

            Cleanup.DeleteDirectory(workingDirectory);
        }

        [Fact]
        public void ShouldIgnoreInsignificantCommits()
        {
            var workingDirectory = TempDir.Create();
            using var tempRepository = TempRepository.Create(workingDirectory);
            
            TempCsProject.Create(workingDirectory);
            
            var workingFilePath = Path.Join(workingDirectory, "hello.txt");

            // Create and commit a test file
            File.WriteAllText(workingFilePath, "First line of text");
            CommitAll(tempRepository);

            // Run versionize
            var workingCopy = WorkingCopy.Discover(workingDirectory);
            workingCopy.Versionize();

            // Add insignificant change
            File.AppendAllText(workingFilePath, "This is another line of text");
            CommitAll(tempRepository, "chore: Added line of text");

            // Get last commit
            var lastCommit = tempRepository.Head.Tip;

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

            lastCommit.ShouldBe(tempRepository.Head.Tip);

            // Cleanup
            Cleanup.DeleteDirectory(workingDirectory);
        }

        private static void CommitAll(IRepository repository, string message = "feat: Initial commit")
        {
            var author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
            Commands.Stage(repository, "*");
            repository.Commit(message, author, author);
        }
    }
}
