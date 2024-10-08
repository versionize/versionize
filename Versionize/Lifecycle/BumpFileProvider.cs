using Versionize.BumpFiles;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public sealed class BumpFileProvider
{
    public static IBumpFile GetBumpFile(Options options)
    {
        return options.FileType switch
        {
            BumpFileType.DotNet => GetProjectGroup(options),
            _ => throw new NotImplementedException($"Bump file type {options.FileType} is not implemented")
        };
    }

    private static IBumpFile GetProjectGroup(Options options)
    {
        var projectGroup = Projects.Discover(options.WorkingDirectory);

        if (options.TagOnly)
        {
            return projectGroup;
        }

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
        public bool TagOnly { get; init; }
        public string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                FileType = BumpFileType.DotNet,
                TagOnly = versionizeOptions.TagOnly,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}
