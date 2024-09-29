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
        var (success,version, error) = ReadVersion(projectFile);
        
        if (!success)
        {
            throw new InvalidOperationException(error);
        }

        return new Project(projectFile, version);
    }

    public static bool IsVersionable(string projectFile)
    {
        try
        {
            var (success, _, _) = ReadVersion(projectFile);
            return success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static (bool, SemanticVersion, string) ReadVersion(string projectFile)
    {
        var doc =  ReadProject(projectFile);

        var versionString = SelectVersionNode(doc)?.InnerText;

        if (string.IsNullOrWhiteSpace(versionString))
        {
            return (false, 
                null, 
                $"Project {projectFile} contains no or an empty <Version> XML Element. Please add one if you want to version this project - for example use <Version>1.0.0</Version>"
                );
        }

        try
        {
            return (true, SemanticVersion.Parse(versionString),null);
        }
        catch (Exception)
        {
            return (false, null,
            $"Project {projectFile} contains an invalid version {versionString}. Please fix the currently contained version - for example use <Version>1.0.0</Version>");
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
