using Versionize.Config;

namespace Versionize.BumpFiles;

public static class BumpFileTypeDetector
{
    /// <summary>
    /// Detects the type of bump file based on the project structure in the specified directory.
    /// </summary>
    /// <remarks>
    /// This class provides functionality to automatically detect whether a project is a .NET project,
    /// Unity project, or neither by examining the directory structure and file patterns.
    /// Returns <see cref="BumpFileType.None"/> if <paramref name="tagOnly"/> is true.
    /// </remarks>
    public static BumpFileType GetType(string cwd, bool tagOnly)
    {
        if (tagOnly)
        {
            return BumpFileType.None;
        }

        if (IsUnityProjectRecursive(cwd))
        {
            return BumpFileType.Unity;
        }

        if (IsDotnetProject(cwd))
        {
            return BumpFileType.Dotnet;
        }

        return BumpFileType.None;
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
}
