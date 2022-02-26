using System.Xml;
using NuGet.Versioning;

namespace Versionize;

public class Project
{
    public string ProjectFile { get; }
    public SemanticVersion Version { get; }

    private Project(string projectFile, SemanticVersion version)
    {
        ProjectFile = projectFile;
        Version = version;
    }

    public static Project Create(string projectFile)
    {
        var version = ReadVersion(projectFile);

        return new Project(projectFile, version);
    }

    public static bool IsVersionable(string projectFile)
    {
        try
        {
            ReadVersion(projectFile);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static SemanticVersion ReadVersion(string projectFile)
    {
        var doc =  ReadProject(projectFile);

        var versionString = SelectVersionNode(doc)?.InnerText;

        if (string.IsNullOrWhiteSpace(versionString))
        {
            throw new InvalidOperationException($"Project {projectFile} contains no or an empty <Version> XML Element. Please add one if you want to version this project - for example use <Version>1.0.0</Version>");
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

    public void WriteVersion(SemanticVersion nextVersion)
    {
        var doc = ReadProject(ProjectFile);

        var versionElement = SelectVersionNode(doc);
        versionElement.InnerText = nextVersion.ToString();

        doc.Save(ProjectFile);
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
