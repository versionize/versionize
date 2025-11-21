using NuGet.Versioning;

namespace Versionize.BumpFiles;

public sealed class NullBumpFile : IBumpFile
{
    public static readonly NullBumpFile Default = new();

    private NullBumpFile() { }

    public SemanticVersion Version => new(0, 0, 0);

    public void WriteVersion(SemanticVersion nextVersion)
    {
        // Do nothing
    }

    public IEnumerable<string> GetFilePaths()
    {
        return [];
    }
}
