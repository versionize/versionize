namespace Versionize.CommandLine;

public class PlatformAbstractions : IPlatformAbstractions
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

        WriteLine(new ColoredText { Text = message, Color = color });
    }

    public void WriteLine(params ColoredText[] messages)
    {
        if (Verbosity == LogLevel.Silent)
        {
            return;
        }

        foreach (var message in messages)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = message.Color;
            Console.Write(message.Text);
            Console.ForegroundColor = oldColor;
        }

        Console.WriteLine();
    }
}
