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
    private readonly IEnumerable<AssemblyInfoBumpFile> _assemblyInfoFiles;

    private DotnetBumpFile(IEnumerable<DotnetBumpFileProject> projects, IEnumerable<AssemblyInfoBumpFile> assemblyInfoFiles)
    {
        _projects = projects;
        _assemblyInfoFiles = assemblyInfoFiles;
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

        var allFiles = projectGroup.GetFilePaths().ToList();
        Information(InfoMessages.DiscoveredVersionableProjects(allFiles.Count));
        foreach (var file in allFiles)
        {
            Information(InfoMessages.ProjectFile(file));
        }

        return projectGroup;
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        foreach (var project in _projects)
        {
            project.WriteVersion(nextVersion);
        }

        foreach (var assemblyInfo in _assemblyInfoFiles)
        {
            assemblyInfo.WriteVersion(nextVersion);
        }
    }

    public IEnumerable<string> GetFilePaths()
    {
        var projectFiles = _projects.Select(project => project.ProjectFile);
        var assemblyInfoFiles = _assemblyInfoFiles.Select(assemblyInfo => assemblyInfo.FilePath);
        return projectFiles.Concat(assemblyInfoFiles);
    }

    private static DotnetBumpFile Discover(string workingDirectory, string? versionElement = null)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj", "*.esproj", "*.props" };

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        var projects = filters
            .SelectMany(filter => Directory.GetFiles(workingDirectory, filter, options))
            .Where(file => DotnetBumpFileProject.IsVersionable(file, versionElement))
            .Select(file => DotnetBumpFileProject.Create(file, versionElement))
            .ToList();

        var assemblyInfoFiles = projects
            .Select(project => AssemblyInfoBumpFile.TryCreate(Path.GetDirectoryName(project.ProjectFile)!, versionElement ?? "AssemblyVersion"))
            .OfType<AssemblyInfoBumpFile>()
            .ToList();

        return new DotnetBumpFile(projects, assemblyInfoFiles);
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
