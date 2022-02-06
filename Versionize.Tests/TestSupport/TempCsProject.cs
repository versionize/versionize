using System.Xml;

namespace Versionize.Tests.TestSupport;

public static class TempCsProject
{
    public static string Create(string tempDir, string version = "1.0.0")
    {
        Directory.CreateDirectory(tempDir);

        var projectDirName = new DirectoryInfo(tempDir).Name;
        var csProjFile = $"{tempDir}/{projectDirName}.csproj";

        // Create .net project
        var projectFileContents =
            $@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>{version}</Version>
    </PropertyGroup>
</Project>";
        File.WriteAllText(csProjFile, projectFileContents);

        // Add version string to csproj
        var doc = new XmlDocument { PreserveWhitespace = true };

        doc.Load(csProjFile);

        var projectNode = doc.SelectSingleNode("/Project/PropertyGroup");
        var versionNode = doc.CreateNode("element", "Version", "");
        versionNode.InnerText = version;
        projectNode.AppendChild(versionNode);
        using var tw = new XmlTextWriter(csProjFile, null);
        doc.Save(tw);

        return csProjFile;
    }
}
