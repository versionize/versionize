using NuGet.Versioning;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public sealed class BumpFileUpdater
{
    public void Update(
        VersionizeOptions options,
        SemanticVersion nextVersion,
        IBumpFile bumpFile)
    {
        if (options.SkipCommit)
        {
            return;
        }
        if (options.TagOnly)
        {
            return;
        }

        Step($"bumping version from {bumpFile.Version} to {nextVersion} in projects");

        if (options.DryRun)
        {
            return;
        }

        bumpFile.Update(options, nextVersion);
    }
}
