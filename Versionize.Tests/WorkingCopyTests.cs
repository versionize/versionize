using System.Threading;
using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using System.Xml;
using System.Linq;
using System.Diagnostics;

namespace Versionize.Tests
{
    public class WorkingCopyTests
    {
        // Thanks @dtb https://stackoverflow.com/a/1344242
        private static Random _random = new Random();
        private static string _RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

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
            var directoryWithoutWorkingCopy = Path.Combine(Path.GetTempPath(), "ShouldExitIfNoWorkingCopyCouldBeDiscovered");
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
            // Set directory and filenames
            var random = new Random();
            var tempDir = $"{Path.GetTempPath()}{_RandomString(10)}";
            var tempFile = $"{tempDir}/{_RandomString(10)}.txt";
            var csProjFile = $"{tempDir}/{Path.GetFileName(tempDir)}.csproj";
            Directory.CreateDirectory(tempDir);
            Repository.Init(tempDir);

            // Make sure we have a git author or versionize will fail to make a commit
            var gitConfigCmd = new ProcessStartInfo("git", "config user.name VersionizeTest");
            gitConfigCmd.WorkingDirectory = tempDir;
            Process.Start(gitConfigCmd).WaitForExit();
            gitConfigCmd.Arguments = "config user.email noreply@versionize.com";
            Process.Start(gitConfigCmd).WaitForExit();

            // Initialize git repo
            var repo = new Repository(tempDir);

            // Create .net project
            Process.Start("dotnet", $"new console -o {tempDir}").WaitForExit();

            // Add version string to csproj
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;

            try
            {
                doc.Load(csProjFile);
            }
            catch (Exception)
            {
                throw;
            }

            var projectNode = doc.SelectSingleNode("/Project/PropertyGroup");
            var versionNode = doc.CreateNode("element", "Version", "");
            versionNode.InnerText = "0.0.0";
            projectNode.AppendChild(versionNode);
            using (var tw = new XmlTextWriter(csProjFile, null))
            {
                doc.Save(tw);
            }

            // Create author
            var author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);

            // Create and commit testfile
            File.WriteAllText(tempFile, "First line of text");
            Commands.Stage(repo, "*");
            repo.Commit("feat: Initial commit", author, author);

            // Run versionize
            var workingCopy = WorkingCopy.Discover(tempDir);
            workingCopy.Versionize();

            // Add insignificant change
            using (var sw = File.AppendText(tempFile))
            {
                sw.WriteLine("This is another line of text");
            }
            Commands.Stage(repo, "*");
            author = new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
            repo.Commit("chore: Added line of text", author, author);

            // Get last commit
            var lastCommit = repo.Head.Tip;

            // Run versionize, ignoring insignificant commits
            try
            {
                workingCopy.Versionize(ignoreInsignificant: true);
            }
            catch (CommandLineExitException ex)
            {
                Assert.Equal(0, ex.ExitCode);
            }
            Assert.Equal(lastCommit, repo.Head.Tip);
            repo.Dispose();

            // Cleanup
            // Need to cleanup readonly attributes first...
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var attribs = File.GetAttributes(file);
                if (attribs.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(file, attribs & ~FileAttributes.ReadOnly);
                }
            }
            Directory.Delete(tempDir, true);
        }
    }
}
