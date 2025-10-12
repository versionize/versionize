using System.Xml;
using NuGet.Versioning;

namespace Versionize.BumpFiles;

public sealed class DotnetBumpFileProject
{
    public string ProjectFile { get; }
    public SemanticVersion Version { get; }
    public bool BumpOnlyFileVersion { get; }

    private DotnetBumpFileProject(string projectFile, SemanticVersion version, bool bumpOnlyFileVersion = false)
    {
        ProjectFile = projectFile;
        Version = version;
        BumpOnlyFileVersion = bumpOnlyFileVersion;
    }

    public static DotnetBumpFileProject Create(string projectFile, bool bumpOnlyFileVersion = false)
    {
        var (success, version, error) = ReadVersion(projectFile, bumpOnlyFileVersion);

        if (!success)
        {
            throw new InvalidOperationException(error);
        }

        return new DotnetBumpFileProject(projectFile, version!, bumpOnlyFileVersion);
    }

    public static bool IsVersionable(string projectFile, bool bumpOnlyFileVersion = false)
    {
        try
        {
            var (success, _, _) = ReadVersion(projectFile, bumpOnlyFileVersion);
            return success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static (bool Success, SemanticVersion? Version, string? Error) ReadVersion(string projectFile, bool bumpOnlyFileVersion = false)
    {
        var doc = ReadProject(projectFile);
        var elementName = bumpOnlyFileVersion ? "FileVersion" : "Version";

        var versionString = SelectVersionNode(doc, bumpOnlyFileVersion)?.InnerText;

        if (string.IsNullOrWhiteSpace(versionString))
        {
            return (
                false,
                null,
                $"Project {projectFile} contains no or an empty <{elementName}> XML Element. Please add one if you want to version this project - for example use <{elementName}>1.0.0</{elementName}>");
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
                $"Project {projectFile} contains an invalid version {versionString}. Please fix the currently contained version - for example use <{elementName}>1.0.0</{elementName}>");
        }
    }

    public void WriteVersion(SemanticVersion nextVersion)
    {
        var doc = ReadProject(ProjectFile);
        var elementName = BumpOnlyFileVersion ? "FileVersion" : "Version";
        var versionElement = SelectVersionNode(doc, BumpOnlyFileVersion) ??
            throw new InvalidOperationException($"Project {ProjectFile} does not contain a <{elementName}> XML Element. Please add one if you want to version this project - for example use <{elementName}>1.0.0</{elementName}>");
        versionElement.InnerText = nextVersion.ToString();

        doc.Save(ProjectFile);
    }

    private static XmlNode? SelectVersionNode(XmlDocument doc, bool bumpOnlyFileVersion = false)
    {
        var elementName = bumpOnlyFileVersion ? "FileVersion" : "Version";
        return doc.SelectSingleNode($"/*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='{elementName}']");
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
