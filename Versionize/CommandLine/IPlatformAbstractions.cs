namespace Versionize.CommandLine;

public interface IPlatformAbstractions
{
    void Exit(int exitCode);
    void WriteLine(string message, ConsoleColor color);
    void WriteLine(params (string text, ConsoleColor color)[] messages);

    LogLevel Verbosity { get; set; }
}

public enum LogLevel
{
    Silent = 0,
    All = 1
}
