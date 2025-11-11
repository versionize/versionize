using System.Xml;
using NuGet.Versioning;
using Versionize.CommandLine;

namespace Versionize.BumpFiles;

public sealed class DotnetBumpFileProject
{
    public string ProjectFile { get; }
    public SemanticVersion Version { get; }
    public string VersionElement { get; }

    private DotnetBumpFileProject(string projectFile, SemanticVersion version, string? versionElement = null)
    {
        ProjectFile = projectFile;
        Version = version;
        VersionElement = string.IsNullOrEmpty(versionElement) ? "Version" : versionElement;
    }

    public static DotnetBumpFileProject Create(string projectFile, string? versionElement = null)
    {
        var (success, version, error) = ReadVersion(projectFile, versionElement);

        if (!success)
        {
            throw new VersionizeException(error ?? ErrorMessages.ProjectInvalidXmlFile(projectFile), 1);
        }

        return new DotnetBumpFileProject(projectFile, version!, versionElement);
    }

    public static bool IsVersionable(string projectFile, string? versionElement = null)
    {
        try
        {
            var (success, _, _) = ReadVersion(projectFile, versionElement);
            return success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static (bool Success, SemanticVersion? Version, string? Error) ReadVersion(string projectFile, string? versionElement = null)
    {
        var doc = ReadProject(projectFile);
        versionElement = string.IsNullOrEmpty(versionElement) ? "Version" : versionElement;

        var versionString = SelectVersionNode(doc, versionElement)?.InnerText;

        if (string.IsNullOrWhiteSpace(versionString))
        {
            return (
                false,
                null,
                ErrorMessages.ProjectMissingOrEmptyVersionElement(projectFile, versionElement));
        }

        try
        {
            return (true, SemanticVersion.Parse(versionString), null);
        }
        catch (Exception)
        {
            return (
                false,
                null,
                ErrorMessages.ProjectInvalidVersionValue(projectFile, versionString, versionElement));
        }
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        var doc = ReadProject(ProjectFile);
        var versionElement = SelectVersionNode(doc, VersionElement) ??
            throw new VersionizeException(ErrorMessages.ProjectMissingVersionElement(ProjectFile, VersionElement), 1);
        versionElement.InnerText = nextVersion.ToString();

        doc.Save(ProjectFile);
    }

    private static XmlNode? SelectVersionNode(XmlDocument doc, string versionElement)
    {
        return doc.SelectSingleNode($"/*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='{versionElement}']");
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
            throw new VersionizeException(ErrorMessages.ProjectInvalidXmlFile(projectFile), 1);
        }

        return doc;
    }
}
