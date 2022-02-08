using System.Drawing;
using Colorful;

namespace Versionize.CommandLine;

public interface IPlatformAbstractions
{
    void Exit(int exitCode);
    void WriteLine(string message, Color color);
    void WriteLineFormatted(string message, Color color, Formatter[] messageFormatters);

    LogLevel Verbosity { get; set; }
}

public enum LogLevel
{
    Silent = 0,
    All = 1
}
