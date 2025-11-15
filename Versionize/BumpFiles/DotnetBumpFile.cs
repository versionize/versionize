using NuGet.Versioning;
using Versionize.CommandLine;

using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.BumpFiles;

/// <summary>
/// A composite of one or more versionable <see cref="DotnetBumpFileProject"/> instances.
/// </summary>
public sealed class DotnetBumpFile : IBumpFile
{
    private readonly IEnumerable<DotnetBumpFileProject> _projects;

    private DotnetBumpFile(IEnumerable<DotnetBumpFileProject> projects)
    {
        _projects = projects;
    }

    public SemanticVersion Version => _projects.First().Version;

    public static DotnetBumpFile Create(string workingDirectory, string? versionElement = null)
    {
        var projectGroup = Discover(workingDirectory, versionElement);
        versionElement = string.IsNullOrEmpty(versionElement) ? "Version" : versionElement;

        if (projectGroup.IsEmpty())
        {
            throw new VersionizeException(ErrorMessages.NoVersionableProjects(workingDirectory, versionElement), 1);
        }

        if (projectGroup.HasInconsistentVersioning())
        {
            throw new VersionizeException(ErrorMessages.InconsistentProjectVersions(workingDirectory, versionElement), 1);
        }

        Information(InfoMessages.DiscoveredVersionableProjects(projectGroup.GetFilePaths().Count()));
        foreach (var project in projectGroup.GetFilePaths())
        {
            Information(InfoMessages.ProjectFile(project));
        }

        return projectGroup;
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

    private static DotnetBumpFile Discover(string workingDirectory, string? versionElement = null)
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

        return new DotnetBumpFile(projects);
    }

    private bool IsEmpty() => !_projects.Any();

    private bool HasInconsistentVersioning()
    {
        var firstProjectVersion = _projects.FirstOrDefault()?.Version;

        if (firstProjectVersion == null)
        {
            return true;
        }

        return _projects.Any(p => !p.Version.Equals(firstProjectVersion));
    }
}
