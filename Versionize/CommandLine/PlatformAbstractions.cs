using System;
using System.Drawing;
using Colorful;
using Console = Colorful.Console;

namespace Versionize.CommandLine
{
    public class PlatformAbstractions : IPlatformAbstractions
    {
        public void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string message, Color color)
        {
            Console.WriteLine(message, color);
        }

        public void WriteLineFormatted(string message, Color color, Formatter[] messageFormatters)
        {
            Console.WriteLineFormatted(message, color, messageFormatters);
        }
    }
}
