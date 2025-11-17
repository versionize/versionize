using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.CommandLine;
using Versionize.Config;

using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.Pipeline.VersionizeSteps;

public class UpdateBumpFileStep : IPipelineStep<BumpVersionResult, UpdateBumpFileStep.Options, BumpVersionResult>
{
    public BumpVersionResult Execute(BumpVersionResult input, Options options)
    {
        return new BumpVersionResult
        {
            Repository = input.Repository,
            BumpFile = input.BumpFile,
            Version = input.Version,
            IsFirstRelease = input.IsFirstRelease,
            Commits = input.Commits,
            BumpedVersion = input.BumpedVersion,
        };
    }

    public static void Update(Options options, SemanticVersion nextVersion, IBumpFile bumpFile)
    {
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

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public bool DryRun { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
            };
        }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
