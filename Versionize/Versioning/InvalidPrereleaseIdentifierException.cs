using System.Runtime.Serialization;

namespace Versionize.Versioning;

[Serializable]
public class InvalidPrereleaseIdentifierException : Exception
{
    public InvalidPrereleaseIdentifierException()
    {
    }

    public InvalidPrereleaseIdentifierException(string message) : base(message)
    {
    }

    public InvalidPrereleaseIdentifierException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidPrereleaseIdentifierException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
