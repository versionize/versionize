using NuGet.Versioning;

namespace Versionize;

public interface IVersionSource
{
    bool IsVersionable { get; }
    SemanticVersion Version { get; }
    string FilePath {get;}
    void WriteVersion(SemanticVersion nextVersion);
}
