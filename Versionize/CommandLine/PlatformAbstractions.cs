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

        WriteInColor(Console.WriteLine, message, color);
    }

    public void WriteLineFormatted(string message, ConsoleColor color, Formatter[] messageFormatters)
    {
        if (Verbosity == LogLevel.Silent)
        {
            return;
        }

        var buffer = message.ToCharArray();
        int startIndex = 0;
        for (int i = 0; i < messageFormatters.Length; ++i)
        {
            var replacementText = messageFormatters[i].Target;
            var replacementColor = messageFormatters[i].Color;
            var replacementIndex = message.IndexOf($"{{{i}}}", startIndex);

            if (replacementIndex == -1)
            {
                throw new InvalidOperationException($"{{{i}}} is not present in the formatted message: {message}.");
            }
            if (replacementIndex > 0)
            {
                // Write non-formatted text (up until the formatted part begins).
                var count = replacementIndex - startIndex;
                WriteInColor(Console.Write, buffer, startIndex, count, color);
            }

            // Write formatted text.
            WriteInColor(Console.Write, replacementText, replacementColor);
            int digits = i < 10 ? 1 : (int)Math.Floor(Math.Log10(i) + 1);
            startIndex = replacementIndex + 2 + digits;
        }

        // Write remaining non-formatted text.
        var remainingCount = message.Length - startIndex;
        WriteInColor(Console.WriteLine, buffer, startIndex, remainingCount, color);
    }

    private static void WriteInColor<T>(Action<T> action, T target, ConsoleColor color)
    {
        var oldColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        action.Invoke(target);
        System.Console.ForegroundColor = oldColor;
    }

    private static void WriteInColor(Action<char[], int, int> action, char[] buffer, int index, int count, ConsoleColor color)
    {
        var oldColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        action.Invoke(buffer, index, count);
        System.Console.ForegroundColor = oldColor;
    }
}

public class Formatter
{
    public Formatter(string target, ConsoleColor color)
    {
        Target = target;
        Color = color;
    }

    public string Target { get; set; }
    public ConsoleColor Color { get; set; }
}
