using System;
using System.Drawing;
using Colorful;
using Console = Colorful.Console;

namespace Versionize.CommandLine
{

    public interface IPlatformAbstractions
    {
        void Exit(int exitCode);
        void WriteLine(string message, Color color);
        void WriteLineFormatted(string message, Color color, Formatter[] messageFormatters);
    }
}
