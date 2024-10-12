using NuGet.Versioning;

namespace Versionize;

public interface IBumpFile
{
    public SemanticVersion Version { get; }
    
    void WriteVersion(SemanticVersion nextVersion);

    IEnumerable<string> GetFilePaths();
}
