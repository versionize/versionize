using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Tests
{
    public class ProgramTests
    {
        public ProgramTests()
        {
            CommandLineUI.Platform = new TestPlatformAbstractions();
        }

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
