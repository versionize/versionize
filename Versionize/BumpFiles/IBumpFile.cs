using NuGet.Versioning;

namespace Versionize.BumpFiles;

public interface IBumpFile
{
    SemanticVersion Version { get; }

    void WriteVersion(SemanticVersion nextVersion);

    IEnumerable<string> GetFilePaths();
}
