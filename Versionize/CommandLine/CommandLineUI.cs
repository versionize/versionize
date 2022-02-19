using System.Drawing;

namespace Versionize.CommandLine;

public static class CommandLineUI
{
    public static IPlatformAbstractions Platform { get; set; } = new PlatformAbstractions { Verbosity = LogLevel.All };

    public static int Exit(string message, int code)
    {
        Platform.WriteLine(message, ConsoleColor.Red);
        Platform.Exit(code);

        return code;
    }

    public static void Information(string message)
    {
        Platform.WriteLine(message, ConsoleColor.Gray);
    }

    public static void Step(string message)
    {
        var messageFormatters = new Formatter[]
        {
            new Formatter("√", ConsoleColor.Green),
            new Formatter(message, ConsoleColor.Gray),
        };


        Platform.WriteLineFormatted("{0} {1}", ConsoleColor.White, messageFormatters);
    }

    public static void DryRun(string message)
    {
        Platform.WriteLine("\n---", ConsoleColor.Gray);
        Platform.WriteLine(message, ConsoleColor.DarkGray);
        Platform.WriteLine("---\n", ConsoleColor.Gray);
    }

    public static LogLevel Verbosity { get => Platform.Verbosity; set => Platform.Verbosity = value; }
}
