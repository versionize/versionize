using System.Collections.Generic;
using System.Drawing;
using Colorful;
using Versionize.CommandLine;

namespace Versionize.Tests.TestSupport
{
    public class TestPlatformAbstractions : IPlatformAbstractions
    {
        public LogLevel Verbosity { get; set; }
        public List<string> Messages { get; } = new List<string>();
        
        public void Exit(int exitCode)
        {
            throw new CommandLineExitException(exitCode);
        }

        public void WriteLine(string message, Color color)
        {
            Messages.Add(message);
        }

        public void WriteLineFormatted(string message, Color color, Formatter[] messageFormatters)
        {
            Messages.Add(message);
        }
    }
}
