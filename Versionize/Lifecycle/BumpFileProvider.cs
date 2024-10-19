using Versionize.BumpFiles;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.Lifecycle;

public sealed class BumpFileProvider
{
    public static IBumpFile GetBumpFile(Options options)
    {
        return options.FileType switch
        {
            BumpFileType.DotNet => GetProjectGroup(options),
            BumpFileType.None => new NullBumpFile(),
            _ => throw new NotImplementedException($"Bump file type {options.FileType} is not implemented")
        };
    }

    private static IBumpFile GetProjectGroup(Options options)
    {
        var projectGroup = Projects.Discover(options.WorkingDirectory);

        if (projectGroup.IsEmpty())
        {
            Exit($"Could not find any projects files in {options.WorkingDirectory} that have a <Version> defined in their csproj file.", 1);
        }

        if (projectGroup.HasInconsistentVersioning())
        {
            Exit($"Some projects in {options.WorkingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned", 1);
        }

        Information($"Discovered {projectGroup.GetFilePaths().Count()} versionable projects");
        foreach (var project in projectGroup.GetFilePaths())
        {
            Information($"  * {project}");
        }

        return projectGroup;
    }

    public enum BumpFileType
    {
        None,
        DotNet
    }

    public sealed class Options
    {
        public BumpFileType FileType { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                // TODO: Assign value from VersionizeOptions when implemented
                FileType = versionizeOptions.TagOnly ? BumpFileType.None : BumpFileType.DotNet,
                WorkingDirectory = versionizeOptions.WorkingDirectory ??
                    throw new InvalidOperationException(nameof(versionizeOptions.WorkingDirectory)),
            };
        }
    }
}
