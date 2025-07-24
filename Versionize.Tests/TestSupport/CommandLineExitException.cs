namespace Versionize.Tests.TestSupport;

public class CommandLineExitException(int exitCode) : Exception
{
    public int ExitCode { get; } = exitCode;
}
