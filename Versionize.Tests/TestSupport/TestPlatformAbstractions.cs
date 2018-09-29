using System.Drawing;
using Colorful;
using Versionize.CommandLine;

namespace Versionize.Tests.TestSupport
{
    public class TestPlatformAbstractions : IPlatformAbstractions
    {
        public void Exit(int exitCode)
        {
            throw new CommandLineExitException(exitCode);
        }

        public void WriteLine(string message, Color color)
        {
            
        }

        public void WriteLineFormatted(string message, Color color, Formatter[] messageFormatters)
        {
           
        }
    }
}
