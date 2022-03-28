using NuGet.Versioning;

namespace Versionize;

public class VersionSources
{
    private readonly IEnumerable<IVersionSource> _projects;

    private VersionSources(IEnumerable<IVersionSource> projects)
    {
        _projects = projects;
    }

    public IEnumerable<IVersionSource> Versionables
    {
        get { return _projects.Where(p => p.IsVersionable); }
    }

    public SemanticVersion Version { get => Versionables.First().Version; }

    public bool HasInconsistentVersioning()
    {
        var firstProjectVersion = Versionables.FirstOrDefault()?.Version;

        if (firstProjectVersion == null)
        {
            return true;
        }

        return Versionables.Any(p => !p.Version.Equals(firstProjectVersion));
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        foreach (var versionSource in Versionables)
        {
            versionSource.WriteVersion(nextVersion);
        }
    }

    public static VersionSources Discover(string workingDirectory)
    {
        var versionSources = new List<IVersionSource>();
        versionSources.AddRange(MsBuildVersionSource.Discover(workingDirectory));

        return new VersionSources(versionSources);
    }
}
