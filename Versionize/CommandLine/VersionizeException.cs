namespace Versionize.CommandLine;

public sealed class VersionizeException : Exception
{
    public int ExitCode { get; }

    public VersionizeException(string message, int exitCode = 1)
        : base(message)
    {
        ExitCode = exitCode;
    }

    public VersionizeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
