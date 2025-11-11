namespace Versionize.CommandLine;

public static class CommandLineUI
{
    public static IPlatformAbstractions Platform { get; set; } = new PlatformAbstractions { Verbosity = LogLevel.All };

    public static int Exit(string message, int code)
    {
        if (Verbosity >= LogLevel.Error)
        {
            Platform.WriteLine(message, ConsoleColor.Red);
        }

        Platform.Exit(code);

        return code;
    }

    public static void Information(string message)
    {
        if (Verbosity < LogLevel.All)
        {
            return;
        }

        Platform.WriteLine(message, ConsoleColor.Gray);
    }

    public static void Step(string message)
    {
        if (Verbosity < LogLevel.All)
        {
            return;
        }

        Platform.WriteLine(
            ("√", ConsoleColor.Green),
            (" ", ConsoleColor.White),
            (message, ConsoleColor.Gray));
    }

    public static void DryRun(string message)
    {
        if (Verbosity < LogLevel.All)
        {
            return;
        }

        Platform.WriteLine("\n---", ConsoleColor.Gray);
        Platform.WriteLine(message, ConsoleColor.DarkGray);
        Platform.WriteLine("---\n", ConsoleColor.Gray);
    }

    public static LogLevel Verbosity
    {
        get => Platform.Verbosity;
        set => Platform.Verbosity = value;
    }
}
