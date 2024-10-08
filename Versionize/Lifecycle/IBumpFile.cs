using NuGet.Versioning;
using Versionize.Config;

namespace Versionize;

public interface IBumpFile
{
    public SemanticVersion Version { get; }
    
    void Update(
        Options options,
        SemanticVersion nextVersion);

    IEnumerable<string> GetFilePaths();
    
    public sealed class Options
    {
        public bool SkipCommit { get; init; }
        public bool TagOnly { get; init; }
        public bool DryRun { get; init; }

        public static implicit operator IBumpFile.Options(VersionizeOptions versionizeOptions)
        {
            return new IBumpFile.Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipCommit = versionizeOptions.SkipCommit,
                TagOnly = versionizeOptions.TagOnly,
            };
        }
    }
}
