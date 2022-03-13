using NuGet.Versioning;

namespace Versionize;

public class Projects
{
    private readonly IEnumerable<Project> _projects;

    private Projects(IEnumerable<Project> projects)
    {
        _projects = projects;
    }

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

    public SemanticVersion Version { get => _projects.First().Version; }

    public static Projects Discover(string workingDirectory)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj" };

        var projects = filters.SelectMany(filter => Directory
            .GetFiles(workingDirectory, filter, SearchOption.AllDirectories)
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

    public IEnumerable<string> GetProjectFiles()
    {
        return _projects.Select(project => project.ProjectFile);
    }
}
