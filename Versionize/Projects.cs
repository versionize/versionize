using NuGet.Versioning;

namespace Versionize;

public class Projects
{
    private readonly IEnumerable<Project> _projects;

    private Projects(IEnumerable<Project> projects)
    {
        _projects = projects;
    }

    public IEnumerable<Project> Versionables
    {
        get { return _projects.Where(p => p.IsVersionable); }
    }

    public bool HasInconsistentVersioning()
    {
        var firstProjectVersion = Versionables.FirstOrDefault()?.Version;

        if (firstProjectVersion == null)
        {
            return true;
        }

        return Versionables.Any(p => !p.Version.Equals(firstProjectVersion));
    }

    public SemanticVersion Version { get => Versionables.First().Version; }

    public static Projects Discover(string workingDirectory)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj" };

        var projects = filters.SelectMany(filter => Directory
            .GetFiles(workingDirectory, filter, SearchOption.AllDirectories)
            .Select(Project.Create)
            .ToList()
        );

        return new Projects(projects);
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        foreach (var project in Versionables)
        {
            project.WriteVersion(nextVersion);
        }
    }
}
