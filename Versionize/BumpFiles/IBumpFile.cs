using NuGet.Versioning;

namespace Versionize.BumpFiles;

public interface IBumpFile
{
    public SemanticVersion Version { get; }
    
    void WriteVersion(SemanticVersion nextVersion);

    IEnumerable<string> GetFilePaths();
}
