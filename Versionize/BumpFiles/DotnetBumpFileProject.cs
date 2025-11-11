using System.Xml;
using NuGet.Versioning;

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
            throw new InvalidOperationException(error);
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
                $"Project {projectFile} contains no or an empty <{versionElement}> XML Element. Please add one if you want to version this project - for example use <{versionElement}>1.0.0</{versionElement}>");
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
                $"Project {projectFile} contains an invalid version {versionString}. Please fix the currently contained version - for example use <{versionElement}>1.0.0</{versionElement}>");
        }
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        var doc = ReadProject(ProjectFile);
        var versionElement = SelectVersionNode(doc, VersionElement) ??
            throw new InvalidOperationException($"Project {ProjectFile} does not contain a <{VersionElement}> XML Element. Please add one if you want to version this project - for example use <{VersionElement}>1.0.0</{VersionElement}>");
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
            throw new InvalidOperationException($"Project {projectFile} is not a valid xml project file. Please make sure that you have a valid project file in place!");
        }

        return doc;
    }
}
