using System.Text.RegularExpressions;
using System.Xml;
using Xunit;

namespace Versionize.Tests.TestSupport;

public static class TempProject
{
    public static string CreateFsharpProject(string tempDir, string version = "1.0.0")
    {
        return Create(tempDir, "fsproj", version);
    }

    public static string CreateCsharpProject(string tempDir, string version = "1.0.0")
    {
        return Create(tempDir, "csproj", version);
    }

    public static string CreateVBProject(string tempDir, string version = "1.0.0")
    {
        return Create(tempDir, "vbproj", version);
    }

    public static string CreateProps(string tempDir, string version = "1.0.0")
    {
        return Create(tempDir, "props", version);
    }

    public static string Create(string tempDir, string extension, string version = "1.0.0")
    {
        var projectFileContents =
$@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <Version>{version}</Version>
    </PropertyGroup>
</Project>";

        return CreateFromProjectContents(tempDir, extension, projectFileContents);
    }

    public static string CreateFromProjectContents(string tempDir, string extension, string projectFileContents)
    {
        Directory.CreateDirectory(tempDir);

        var projectDirName = new DirectoryInfo(tempDir).Name;
        var csProjFile = $"{tempDir}/{projectDirName}.{extension}";

        File.WriteAllText(csProjFile, projectFileContents);

        return csProjFile;
    }

    private static readonly string versionPattern = @"bundleVersion:\s*([^\r\n]+)";
    public static string CreateUnityProject(string tempDir, string version = "1.0.0")
    {
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory($"{tempDir}/Assets");
        Directory.CreateDirectory($"{tempDir}/ProjectSettings");

        var sourceFilePath = "TestData/ProjectSettings.asset";
        var targetFilePath = Path.Combine(tempDir, "ProjectSettings", "ProjectSettings.asset");
        File.Copy(sourceFilePath, targetFilePath);
        Assert.True(File.Exists(targetFilePath));
        var fileContents = File.ReadAllText(targetFilePath);
        fileContents = Regex.Replace(fileContents, versionPattern, $"bundleVersion: {version}");
        File.WriteAllText(targetFilePath, fileContents);

        return tempDir;
    }
}
