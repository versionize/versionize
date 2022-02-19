using Versionize.CommandLine;

namespace Versionize.Tests.TestSupport;

public class TestPlatformAbstractions : IPlatformAbstractions
{
    public LogLevel Verbosity { get; set; }
    public List<List<string>> Messages { get; } = new List<List<string>>();

    public void Exit(int exitCode)
    {
        throw new CommandLineExitException(exitCode);
    }

    public void WriteLine(string message, ConsoleColor color)
    {
        Messages.Add(new List<string> { message });
    }

    public void WriteLine(params (string text, ConsoleColor color)[] messages)
    {
        Messages.Add(messages.Select(m => m.text).ToList());
    }
}
