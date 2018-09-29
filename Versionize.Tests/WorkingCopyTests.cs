using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;

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
    }
}
