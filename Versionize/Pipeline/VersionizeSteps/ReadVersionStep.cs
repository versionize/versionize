using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;

namespace Versionize.Pipeline.VersionizeSteps;

public class ReadVersionStep : IPipelineStep<GetBumpFileResult, ReadVersionStep.VersionOptions, ReadVersionResult>
{
    public ReadVersionResult Execute(GetBumpFileResult input, VersionOptions options)
    {
        return new ReadVersionResult
        {
            Repository = input.Repository,
            BumpFile = input.BumpFile,
            Version = GetCurrentVersion(input.Repository, options, input.BumpFile) ?? new SemanticVersion(1, 0, 0),
        };
    }

    private static SemanticVersion? GetCurrentVersion(Repository repository, VersionOptions options, IBumpFile bumpFile)
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

    public sealed class VersionOptions : IConvertibleFromVersionizeOptions<VersionOptions>
    {
        public bool TagOnly { get; init; }
        public required ProjectOptions Project { get; init; }

        public static VersionOptions FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new VersionOptions
            {
                TagOnly = versionizeOptions.BumpFileType == BumpFileType.None,
                Project = versionizeOptions.Project,
            };
        }

        public static implicit operator VersionOptions(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
