using NuGet.Versioning;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.BumpFiles;

/// <summary>
/// A composite of one or more versionable <see cref="DotnetBumpFileProject"/> instances.
/// </summary>
public sealed class DotnetBumpFile : IBumpFile
{
    private readonly IEnumerable<DotnetBumpFileProject> _projects;
    public bool BumpOnlyFileVersion { get; }

    private DotnetBumpFile(IEnumerable<DotnetBumpFileProject> projects, bool bumpOnlyFileVersion = false)
    {
        _projects = projects;
        BumpOnlyFileVersion = bumpOnlyFileVersion;
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

    public static DotnetBumpFile Create(string workingDirectory, bool bumpOnlyFileVersion = false)
    {
        var projectGroup = DotnetBumpFile.Discover(workingDirectory, bumpOnlyFileVersion);
        var elementName = bumpOnlyFileVersion ? "FileVersion" : "Version";

        if (projectGroup.IsEmpty())
        {
            Exit($"Could not find any projects files in {workingDirectory} that have a <{elementName}> defined in their csproj file.", 1);
        }

        if (projectGroup.HasInconsistentVersioning())
        {
            Exit($"Some projects in {workingDirectory} have an inconsistent <{elementName}> defined in their csproj file. Please update all versions to be consistent or remove the <{elementName}> elements from projects that should not be versioned", 1);
        }

        Information($"Discovered {projectGroup.GetFilePaths().Count()} versionable projects");
        foreach (var project in projectGroup.GetFilePaths())
        {
            Information($"  * {project}");
        }

        return projectGroup;
    }

    public static DotnetBumpFile Discover(string workingDirectory, bool bumpOnlyFileVersion = false)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj", "*.esproj", "*.props" };

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        var projects = filters.SelectMany(filter => Directory
            .GetFiles(workingDirectory, filter, options)
            .Where(file => DotnetBumpFileProject.IsVersionable(file, bumpOnlyFileVersion))
            .Select(file => DotnetBumpFileProject.Create(file, bumpOnlyFileVersion))
            .ToList()
        );

        return new DotnetBumpFile(projects, bumpOnlyFileVersion);
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
