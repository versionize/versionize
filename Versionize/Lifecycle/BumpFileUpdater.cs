using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;
using Versionize.CommandLine;

using Input = Versionize.Lifecycle.IBumpFileUpdater.Input;
using Options = Versionize.Lifecycle.IBumpFileUpdater.Options;

namespace Versionize.Lifecycle;

public sealed class BumpFileUpdater : IBumpFileUpdater
{
    public void Update(Input input, Options options)
    {
        var bumpFile = input.BumpFile;
        var nextVersion = input.NewVersion;

        if (bumpFile.Version != new SemanticVersion(0, 0, 0))
        {
            Step(InfoMessages.BumpingVersion(bumpFile.Version.ToString(), nextVersion.ToString()));
        }

        if (options.DryRun)
        {
            return;
        }

        bumpFile.WriteVersion(nextVersion);
    }
}

public interface IBumpFileUpdater
{
    void Update(Input input, Options options);

    sealed class Input
    {
        public required SemanticVersion NewVersion { get; init; }
        public required IBumpFile BumpFile { get; init; }
    }

    sealed class Options
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
