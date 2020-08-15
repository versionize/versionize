using System.Threading;
using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using System.Xml;
using System.Diagnostics;

namespace Versionize.Tests
{
    public class WorkingCopyTests
    {
        public WorkingCopyTests()
        {
            CommandLineUI.Platform = new TestPlatformAbstractions();
        }

        [Fact]
        public void ShouldDiscoverGitWorkingCopies()
        {
            var workingCopy = WorkingCopy.Discover(Directory.GetCurrentDirectory());

            Assert.NotNull(workingCopy);
        }

        [Fact]
        public void ShouldExitIfNoWorkingCopyCouldBeDiscovered()
        {
            var directoryWithoutWorkingCopy =
                Path.Combine(Path.GetTempPath(), "ShouldExitIfNoWorkingCopyCouldBeDiscovered");
            Directory.CreateDirectory(directoryWithoutWorkingCopy);

            Assert.Throws<CommandLineExitException>(() => WorkingCopy.Discover(directoryWithoutWorkingCopy));
        }

        [Fact]
        public void ShouldExitIfWorkingCopyDoesNotExist()
        {
            var directoryWithoutWorkingCopy = Path.Combine(Path.GetTempPath(), "ShouldExitIfWorkingCopyDoesNotExist");

            Assert.Throws<CommandLineExitException>(() => WorkingCopy.Discover(directoryWithoutWorkingCopy));
        }

        [Fact]
        public void ShouldPreformADryRun()
        {
            var workingCopy = WorkingCopy.Discover(Directory.GetCurrentDirectory());
            workingCopy.Versionize(dryrun: true, skipDirtyCheck: true);

            // TODO: Assert messages
        }

        [Fact]
        public void ShouldIgnoreInsignificantCommits()
        {
            var workingDirectory = TempDir.Create();
            using var tempRepository = TempRepository.Create(workingDirectory);
            
            TempCsProject.Create(tempRepository.Info.WorkingDirectory);

            // Create author
            var author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);

            var workingFilePath = Path.Join(workingDirectory, "hello.txt");

            // Create and commit testfile
            File.WriteAllText(workingFilePath, "First line of text");
            Commands.Stage(tempRepository, "*");
            tempRepository.Commit("feat: Initial commit", author, author);

            // Run versionize
            var workingCopy = WorkingCopy.Discover(workingDirectory);
            workingCopy.Versionize();

            // Add insignificant change
            using (var sw = File.AppendText(workingFilePath))
            {
                sw.WriteLine("This is another line of text");
            }

            Commands.Stage(tempRepository, "*");
            author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
            tempRepository.Commit("chore: Added line of text", author, author);

            // Get last commit
            var lastCommit = tempRepository.Head.Tip;

            // Run versionize, ignoring insignificant commits
            try
            {
                workingCopy.Versionize(ignoreInsignificant: true);
            }
            catch (CommandLineExitException ex)
            {
                Assert.Equal(0, ex.ExitCode);
            }

            Assert.Equal(lastCommit, tempRepository.Head.Tip);

            // Cleanup
            Cleanup.DeleteDirectory(workingDirectory);
        }
    }
}
