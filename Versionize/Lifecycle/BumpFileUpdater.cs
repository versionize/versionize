using NuGet.Versioning;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public sealed class BumpFileUpdater
{
    public static void Update(
        Options options,
        SemanticVersion nextVersion,
        IBumpFile bumpFile)
    {
        if (bumpFile.Version != new SemanticVersion(0, 0, 0))
        {
            Step($"bumping version from {bumpFile.Version} to {nextVersion} in projects");
        }

        if (options.DryRun)
        {
            return;
        }

        bumpFile.WriteVersion(nextVersion);
    }

    public sealed class Options
    {
        public bool DryRun { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
            };
        }
    }
}
