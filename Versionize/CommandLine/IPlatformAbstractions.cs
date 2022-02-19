using System.Drawing;

namespace Versionize.CommandLine;

public interface IPlatformAbstractions
{
    void Exit(int exitCode);
    void WriteLine(string message, ConsoleColor color);
    void WriteLineFormatted(string message, ConsoleColor color, Formatter[] messageFormatters);

    LogLevel Verbosity { get; set; }
}

public enum LogLevel
{
    Silent = 0,
    All = 1
}
