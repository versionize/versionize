using Versionize.Config;

using Options = Versionize.BumpFiles.IBumpFileProvider.Options;

namespace Versionize.BumpFiles;

internal sealed class BumpFileProvider : IBumpFileProvider
{
    /// <summary>
    /// Detects the type of bump file based on project structure in the specified directory.
    /// </summary>
    /// <remarks>
    /// Supported types: .NET, Unity, or none.<br/>
    /// Returns <see cref="NullBumpFile"/> if <see cref="Options.SkipBumpFile"/> is true.
    /// </remarks>
    public IBumpFile? GetBumpFile(Options options)
    {
        if (options.SkipBumpFile)
        {
            return null;
        }

        if (IsUnityProjectRecursive(options.WorkingDirectory))
        {
            return UnityBumpFile.Create(options.WorkingDirectory);
        }

        if (IsDotnetProject(options.WorkingDirectory))
        {
            return DotnetBumpFile.Create(options.WorkingDirectory, options.VersionElement);
        }

        return null;
    }

    public static bool IsDotnetProject(string directoryPath)
    {
        return DiscoverProjectFiles(directoryPath).Any();
    }

    public static IEnumerable<string> DiscoverProjectFiles(string directoryPath)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj", "*.esproj", "*.props" };

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        return filters
            .SelectMany(filter => Directory.EnumerateFiles(directoryPath, filter, options))
            .Where(path => !IsBuildOutputPath(path));
    }

    private static bool IsBuildOutputPath(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var segments = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase));
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
}

internal interface IBumpFileProvider
{
    IBumpFile? GetBumpFile(Options options);

    sealed class Options
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
    }
}
