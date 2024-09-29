namespace Versionize.CommandLine;

public sealed class PlatformAbstractions : IPlatformAbstractions
{
    public LogLevel Verbosity { get; set; }

    public void Exit(int exitCode)
    {
        Environment.Exit(exitCode);
    }

    public void WriteLine(string message, ConsoleColor color)
    {
        if (Verbosity == LogLevel.Silent)
        {
            return;
        }

        WriteLine((message, color));
    }

    public void WriteLine(params (string text, ConsoleColor color)[] messages)
    {
        if (Verbosity == LogLevel.Silent)
        {
            return;
        }

        foreach (var (text, color) in messages)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }

        Console.WriteLine();
    }
}
