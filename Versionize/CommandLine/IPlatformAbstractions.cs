using System.Drawing;

namespace Versionize.CommandLine;

public interface IPlatformAbstractions
{
    void Exit(int exitCode);
    void WriteLine(string message, ConsoleColor color);
    void WriteLine(params ColoredText[] messages);

    LogLevel Verbosity { get; set; }
}

public class ColoredText {
    public string Text { get; set; }
    public ConsoleColor Color { get; set; }
}

public enum LogLevel
{
    Silent = 0,
    All = 1
}
