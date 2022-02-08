namespace Versionize.Tests.TestSupport;

public class CommandLineExitException : Exception
{
    public int ExitCode { get; }

    public CommandLineExitException(int exitCode)
    {
        ExitCode = exitCode;
    }
}
