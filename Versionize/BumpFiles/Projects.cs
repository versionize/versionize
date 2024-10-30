using NuGet.Versioning;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.BumpFiles;

public sealed class Projects : IBumpFile
{
    private readonly IEnumerable<Project> _projects;

    private Projects(IEnumerable<Project> projects)
    {
        _projects = projects;
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

    public static Projects Create(string workingDirectory)
    {
        var projectGroup = Projects.Discover(workingDirectory);

        if (projectGroup.IsEmpty())
        {
            Exit($"Could not find any projects files in {workingDirectory} that have a <Version> defined in their csproj file.", 1);
        }

        if (projectGroup.HasInconsistentVersioning())
        {
            Exit($"Some projects in {workingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned", 1);
        }

        Information($"Discovered {projectGroup.GetFilePaths().Count()} versionable projects");
        foreach (var project in projectGroup.GetFilePaths())
        {
            Information($"  * {project}");
        }

        return projectGroup;
    }

    public static Projects Discover(string workingDirectory)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj", "*.esproj", "*.props" };

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        var projects = filters.SelectMany(filter => Directory
            .GetFiles(workingDirectory, filter, options)
            .Where(Project.IsVersionable)
            .Select(Project.Create)
            .ToList()
        );

        return new Projects(projects);
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
