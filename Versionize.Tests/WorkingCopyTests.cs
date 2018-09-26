using System;
using System.IO;
using Xunit;

namespace Versionize.Tests
{
    public class WorkingCopyTests
    {
        [Fact]
        public void ShouldDiscoverGitWorkingCopies()
        {
            var workingCopy = WorkingCopy.Discover(Directory.GetCurrentDirectory());

            Assert.NotNull(workingCopy);
        }

        [Fact]
        public void ShouldThrowIfNoWorkingCopyCouldBeDiscovered()
        {
            var directoryWithoutWorkingCopy = Path.Combine(Path.GetTempPath(), "ShouldThrowIfNoWorkingCopyCouldBeDiscovered");
            Directory.CreateDirectory(directoryWithoutWorkingCopy);

            Assert.Throws<InvalidOperationException>(() => WorkingCopy.Discover(directoryWithoutWorkingCopy));
        }
    }
}
