using Versionize.Config;

namespace Versionize.BumpFiles;

public static class BumpFileTypeDetector
{
    public static BumpFileType GetType(string cwd, bool tagOnly)
    {
        if (tagOnly)
        {
            return BumpFileType.None;
        }

        static bool IsDotnetProject(string directoryPath)
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

        static bool IsUnityProject(string directoryPath)
        {
            return Directory.Exists(Path.Combine(directoryPath, "Assets")) &&
                Directory.Exists(Path.Combine(directoryPath, "ProjectSettings")) &&
                File.Exists(Path.Combine(directoryPath, "ProjectSettings", "ProjectSettings.asset"));
        }

        static bool IsUnityProjectRecursive(string directoryPath)
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
}
