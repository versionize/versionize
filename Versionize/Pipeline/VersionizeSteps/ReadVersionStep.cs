using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;

namespace Versionize.Pipeline.VersionizeSteps;

public class ReadVersionStep : IPipelineStep<GetBumpFileResult, ReadVersionStep.Options, ReadVersionResult>
{
    public ReadVersionResult Execute(GetBumpFileResult input, Options options)
    {
        return new ReadVersionResult
        {
            Repository = input.Repository,
            BumpFile = input.BumpFile,
            Version = GetCurrentVersion(input.Repository, options, input.BumpFile) ?? new SemanticVersion(1, 0, 0),
        };
    }

    private static SemanticVersion? GetCurrentVersion(Repository repository, Options options, IBumpFile bumpFile)
    {
        SemanticVersion? version;
        if (options.TagOnly)
        {
            version = repository.Tags
                .Select(options.Project.ExtractTagVersion)
                .Where(x => x is not null)
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }
        else
        {
            version = bumpFile.Version;
        }

        return version;
    }

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public bool TagOnly { get; init; }
        public required ProjectOptions Project { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                TagOnly = versionizeOptions.BumpFileType == BumpFileType.None,
                Project = versionizeOptions.Project,
            };
        }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
