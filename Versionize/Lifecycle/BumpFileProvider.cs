using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.CommandLine;

namespace Versionize.Lifecycle;

public sealed class BumpFileProvider
{
    public static IBumpFile GetBumpFile(Options options)
    {
        return options.BumpFileType switch
        {
            BumpFileType.Dotnet => DotnetBumpFile.Create(options.WorkingDirectory, options.VersionElement),
            BumpFileType.Unity => UnityBumpFile.Create(options.WorkingDirectory),
            BumpFileType.None => new NullBumpFile(),
            _ => throw new VersionizeException(ErrorMessages.BumpFileTypeNotImplemented(options.BumpFileType.ToString()), 1)
        };
    }

    public sealed class Options
    {
        public BumpFileType BumpFileType { get; init; }
        public string? VersionElement { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                BumpFileType = versionizeOptions.BumpFileType,
                VersionElement = versionizeOptions.Project.VersionElement,
                WorkingDirectory = versionizeOptions.WorkingDirectory ??
                    throw new VersionizeException(nameof(versionizeOptions.WorkingDirectory), 1),
            };
        }
    }
}
