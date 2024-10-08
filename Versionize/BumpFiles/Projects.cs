using NuGet.Versioning;
using Versionize.Config;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.BumpFiles;

public sealed class Projects : IBumpFile
{
    private readonly IEnumerable<Project> _projects;

    private Projects(IEnumerable<Project> projects)
    {
        _projects = projects;
    }

    public SemanticVersion Version { get => _projects.First().Version; }

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

    public void Update(
        IBumpFile.Options options,
        SemanticVersion nextVersion)
    {
        if (options.SkipCommit)
        {
            return;
        }
        if (options.TagOnly)
        {
            return;
        }

        Step($"bumping version from {Version} to {nextVersion} in projects");

        if (options.DryRun)
        {
            return;
        }

        WriteVersion(nextVersion);
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
