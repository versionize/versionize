using System.Drawing;
using Colorful;
using Console = Colorful.Console;

namespace Versionize.CommandLine;

public class PlatformAbstractions : IPlatformAbstractions
{
    public LogLevel Verbosity { get; set; }

    public void Exit(int exitCode)
    {
        Environment.Exit(exitCode);
    }

    public void WriteLine(string message, Color color)
    {
        if (Verbosity == LogLevel.Silent)
        {
            return;
        }

        Console.WriteLine(message, color);
    }

    public void WriteLineFormatted(string message, Color color, Formatter[] messageFormatters)
    {
        if (Verbosity == LogLevel.Silent)
        {
            return;
        }

        Console.WriteLineFormatted(message, color, messageFormatters);
    }
}
