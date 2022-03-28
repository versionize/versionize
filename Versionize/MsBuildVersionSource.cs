using System.Xml;
using NuGet.Versioning;

namespace Versionize;

public class MsBuildVersionSource : IVersionSource
{
    public string FilePath { get; }
    public SemanticVersion Version { get; }

    public bool IsVersionable { get { return Version != null; } }

    private MsBuildVersionSource(string projectFile, SemanticVersion version)
    {
        FilePath = projectFile;
        Version = version;
    }

    public static MsBuildVersionSource Create(string projectFile)
    {
        SemanticVersion version = ReadVersion(projectFile);

        return new MsBuildVersionSource(projectFile, version);
    }

    private static SemanticVersion ReadVersion(string projectFile)
    {
        var doc = ReadProject(projectFile);

        var versionString = SelectVersionNode(doc)?.InnerText;

        if (string.IsNullOrWhiteSpace(versionString))
        {
            return null;
        }

        try
        {
            return SemanticVersion.Parse(versionString);
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Project {projectFile} contains an invalid version {versionString}. Please fix the currently contained version - for example use <Version>1.0.0</Version>");
        }
    }

    public static IEnumerable<IVersionSource> Discover(string workingDirectory)
    {
        var filters = new[] { "*.vbproj", "*.csproj", "*.fsproj" };

        return filters.SelectMany(filter => Directory
            .GetFiles(workingDirectory, filter, SearchOption.AllDirectories)
            .Select(Create)
            .ToList()
        );
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        var doc = ReadProject(FilePath);

        var versionElement = SelectVersionNode(doc);
        versionElement.InnerText = nextVersion.ToString();

        doc.Save(FilePath);
    }

    private static XmlNode SelectVersionNode(XmlDocument doc)
    {
        return doc.SelectSingleNode("/*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='Version']");
    }

    private static XmlDocument ReadProject(string projectFile)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };

        try
        {
            doc.Load(projectFile);
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Project {projectFile} is not a valid xml project file. Please make sure that you have a valid project file in place!");
        }

        return doc;
    }
}
