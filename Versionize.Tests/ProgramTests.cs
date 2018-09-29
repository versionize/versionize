using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;

namespace Versionize.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void ShouldRunVersionizeWithDryRunOption()
        {
            var exitCode = Program.Main(new[] { "--dry-run", "--skip-dirty" });

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ShouldVersionizeDesiredReleaseVersion()
        {
            var exitCode = Program.Main(new[] { "--dry-run", "--skip-dirty", "--release-as", "2.0.0" });

            Assert.Equal(0, exitCode);
        }
    }
}
