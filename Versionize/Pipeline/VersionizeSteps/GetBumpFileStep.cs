using Versionize.BumpFiles;
using Versionize.CommandLine;
using Versionize.Config;

namespace Versionize.Pipeline.VersionizeSteps;

public class GetBumpFileStep : IPipelineStep<InitWorkingCopyResult, GetBumpFileStep.Options, GetBumpFileResult>
{
    public GetBumpFileResult Execute(InitWorkingCopyResult input, Options options)
    {
        return new GetBumpFileResult
        {
            Repository = input.Repository,
            BumpFile = GetBumpFile(options),
        };
    }

    private static IBumpFile GetBumpFile(Options options)
    {
        return options.BumpFileType switch
        {
            BumpFileType.Dotnet => DotnetBumpFile.Create(options.WorkingDirectory, options.VersionElement),
            BumpFileType.Unity => UnityBumpFile.Create(options.WorkingDirectory),
            BumpFileType.None => new NullBumpFile(),
            _ => throw new VersionizeException(ErrorMessages.BumpFileTypeNotImplemented(options.BumpFileType.ToString()), 1)
        };
    }

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public BumpFileType BumpFileType { get; init; }
        public string? VersionElement { get; init; }
        public required string WorkingDirectory { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                BumpFileType = versionizeOptions.BumpFileType,
                VersionElement = versionizeOptions.Project.VersionElement,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
