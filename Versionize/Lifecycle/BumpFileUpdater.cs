using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;
using Versionize.CommandLine;

namespace Versionize.Lifecycle;

public sealed class BumpFileUpdater
{
    public static void Update(
        Options options,
        SemanticVersion nextVersion,
        IBumpFile bumpFile)
    {
        if (bumpFile.Version != new SemanticVersion(0, 0, 0))
        {
            Step(InfoMessages.BumpingVersion(bumpFile.Version.ToNormalizedString(), nextVersion.ToNormalizedString()));
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
