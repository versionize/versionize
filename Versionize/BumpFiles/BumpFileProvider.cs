using Versionize.Config;
using Versionize.Commands;

namespace Versionize.BumpFiles;

internal sealed class BumpFileProvider
{
    /// <summary>
    /// Detects the type of bump file based on project structure in the specified directory.
    /// </summary>
    /// <remarks>
    /// Supported types: .NET, Unity, or none.<br/>
    /// Returns <see cref="NullBumpFile"/> if <see cref="Options.SkipBumpFile"/> is true.
    /// </remarks>
    public static IBumpFile GetBumpFile(Options options)
    {
        if (options.SkipBumpFile)
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

    public sealed class Options
    {
        public bool SkipBumpFile { get; init; }
        public string? VersionElement { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                SkipBumpFile = versionizeOptions.SkipBumpFile,
                VersionElement = versionizeOptions.Project.VersionElement,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }

        public static implicit operator Options(InspectCmdOptions inspectOptions)
        {
            return new Options
            {
                SkipBumpFile = inspectOptions.SkipBumpFile,
                VersionElement = inspectOptions.ProjectOptions.VersionElement,
                WorkingDirectory = inspectOptions.WorkingDirectory,
            };
        }
    }
}
