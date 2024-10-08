namespace Versionize.Versioning;

[Serializable]
public sealed class InvalidPrereleaseIdentifierException : Exception
{
    public InvalidPrereleaseIdentifierException()
    {
    }

    public InvalidPrereleaseIdentifierException(string message)
        : base(message)
    {
    }

    public InvalidPrereleaseIdentifierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
