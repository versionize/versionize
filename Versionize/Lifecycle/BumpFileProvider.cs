using Versionize.BumpFiles;
using Versionize.Config;

namespace Versionize.Lifecycle;

public sealed class BumpFileProvider
{
    public static IBumpFile GetBumpFile(Options options)
    {
        return options.BumpFileType switch
        {
            BumpFileType.Dotnet => DotnetBumpFile.Create(options.WorkingDirectory, options.BumpOnlyFileVersion),
            BumpFileType.Unity => UnityBumpFile.Create(options.WorkingDirectory),
            BumpFileType.None => new NullBumpFile(),
            _ => throw new NotImplementedException($"Bump file type {options.BumpFileType} is not implemented")
        };
    }

    public sealed class Options
    {
        public BumpFileType BumpFileType { get; init; }
        public bool BumpOnlyFileVersion { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                BumpFileType = versionizeOptions.BumpFileType,
                BumpOnlyFileVersion = versionizeOptions.BumpOnlyFileVersion,
                WorkingDirectory = versionizeOptions.WorkingDirectory ??
                    throw new InvalidOperationException(nameof(versionizeOptions.WorkingDirectory)),
            };
        }
    }
}
