using Versionize.BumpFiles;
using Versionize.Config;

namespace Versionize.Pipeline.VersionizeSteps;

public class GetBumpFileStep :
    IPipelineStep<InitWorkingCopyResult, GetBumpFileStep.Options, GetBumpFileResult>
{
    public GetBumpFileResult Execute(InitWorkingCopyResult input, Options options)
    {
        return new GetBumpFileResult
        {
            Repository = input.Repository,
            BumpFile = GetBumpFile(options),
        };
    }

    /// <summary>
    /// Detects the type of bump file based on the project structure in the specified directory.
    /// </summary>
    /// <remarks>
    /// This class provides functionality to automatically detect whether a project is a .NET project,
    /// Unity project, or neither by examining the directory structure and file patterns.
    /// Returns <see cref="BumpFileType.None"/> if <paramref name="tagOnly"/> is true.
    /// </remarks>
    private static IBumpFile GetBumpFile(Options options)
    {
        if (options.TagOnly)
        {
            return NullBumpFile.Default;
        }

        if (IsUnityProjectRecursive(options.WorkingDirectory))
        {
            return UnityBumpFile.Create(options.WorkingDirectory);
        }

        if (IsDotnetProject(options.WorkingDirectory))
        {
            return DotnetBumpFile.Create(options.WorkingDirectory, options.VersionElement);
        }

        return NullBumpFile.Default;
    }

    private static bool IsDotnetProject(string directoryPath)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj", "*.esproj", "*.props" };

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        return filters
            .SelectMany(filter => Directory.EnumerateFiles(directoryPath, filter, options))
            .Any();
    }

    private static bool IsUnityProject(string directoryPath)
    {
        return Directory.Exists(Path.Combine(directoryPath, "Assets")) &&
            Directory.Exists(Path.Combine(directoryPath, "ProjectSettings")) &&
            File.Exists(Path.Combine(directoryPath, "ProjectSettings", "ProjectSettings.asset"));
    }

    private static bool IsUnityProjectRecursive(string directoryPath)
    {
        if (IsUnityProject(directoryPath))
        {
            return true;
        }

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        return Directory
            .EnumerateDirectories(directoryPath, "*", options)
            .Any(IsUnityProject);
    }

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public bool TagOnly { get; init; }
        public string? VersionElement { get; init; }
        public required string WorkingDirectory { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                TagOnly = versionizeOptions.TagOnly,
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
