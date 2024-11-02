using Versionize.CommandLine;

namespace Versionize.Tests.TestSupport;

public class TestPlatformAbstractions : IPlatformAbstractions
{
    public LogLevel Verbosity { get; set; } = LogLevel.All;
    public List<string> Messages { get; } = new List<string>();

    public void Exit(int exitCode)
    {
        throw new CommandLineExitException(exitCode);
    }

    public void WriteLine(string message, ConsoleColor color)
    {
        Messages.Add(message);
    }

    public void WriteLine(params (string text, ConsoleColor color)[] messages)
    {
        Messages.Add(string.Concat(messages.Select(m => m.text)));
    }
}
