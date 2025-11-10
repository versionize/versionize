using NuGet.Versioning;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.BumpFiles;

/// <summary>
/// A composite of one or more versionable <see cref="DotnetBumpFileProject"/> instances.
/// </summary>
public sealed class DotnetBumpFile : IBumpFile
{
    private readonly IEnumerable<DotnetBumpFileProject> _projects;
    public string? VersionElement { get; }

    private DotnetBumpFile(IEnumerable<DotnetBumpFileProject> projects, string? versionElement = null)
    {
        _projects = projects;
        VersionElement = versionElement;
    }

    public SemanticVersion Version => _projects.First().Version;

    public bool IsEmpty()
    {
        return !_projects.Any();
    }

    public bool HasInconsistentVersioning()
    {
        var firstProjectVersion = _projects.FirstOrDefault()?.Version;

        if (firstProjectVersion == null)
        {
            return true;
        }

        return _projects.Any(p => !p.Version.Equals(firstProjectVersion));
    }

    public static DotnetBumpFile Create(string workingDirectory, string? versionElement = null)
    {
        var projectGroup = DotnetBumpFile.Discover(workingDirectory, versionElement);
        versionElement = string.IsNullOrEmpty(versionElement) ? "Version" : versionElement;

        if (projectGroup.IsEmpty())
        {
            Exit($"Could not find any projects files in {workingDirectory} that have a <{versionElement}> defined in their csproj file.", 1);
        }

        if (projectGroup.HasInconsistentVersioning())
        {
            Exit($"Some projects in {workingDirectory} have an inconsistent <{versionElement}> defined in their csproj file. Please update all versions to be consistent or remove the <{versionElement}> elements from projects that should not be versioned", 1);
        }

        Information($"Discovered {projectGroup.GetFilePaths().Count()} versionable projects");
        foreach (var project in projectGroup.GetFilePaths())
        {
            Information($"  * {project}");
        }

        return projectGroup;
    }

    public static DotnetBumpFile Discover(string workingDirectory, string? versionElement = null)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj", "*.esproj", "*.props" };

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        var projects = filters.SelectMany(filter => Directory
            .GetFiles(workingDirectory, filter, options)
            .Where(file => DotnetBumpFileProject.IsVersionable(file, versionElement))
            .Select(file => DotnetBumpFileProject.Create(file, versionElement))
            .ToList()
        );

        return new DotnetBumpFile(projects, versionElement);
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        foreach (var project in _projects)
        {
            project.WriteVersion(nextVersion);
        }
    }

    public IEnumerable<string> GetFilePaths()
    {
        return _projects.Select(project => project.ProjectFile);
    }
}
