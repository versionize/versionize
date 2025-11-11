using System.Text.RegularExpressions;
using Xunit;

namespace Versionize.Tests.TestSupport;

public static class TempProject
{
    public static void CreateFsharpProject(string tempDir, string version = "1.0.0")
    {
        Create(tempDir, "fsproj", version);
    }

    public static void CreateCsharpProject(string tempDir, string version = "1.0.0")
    {
        Create(tempDir, "csproj", version);
    }

    public static void CreateVBProject(string tempDir, string version = "1.0.0")
    {
        Create(tempDir, "vbproj", version);
    }

    public static void CreateProps(string tempDir, string version = "1.0.0")
    {
        Create(tempDir, "props", version);
    }

    public static void Create(string tempDir, string extension, string version = "1.0.0")
    {
        var projectFileContents = $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <Version>{version}</Version>
                </PropertyGroup>
            </Project>
            """;

        CreateFromProjectContents(tempDir, extension, projectFileContents);
    }

    public static void CreateFromProjectContents(string tempDir, string extension, string projectFileContents)
    {
        Directory.CreateDirectory(tempDir);

        var projectDirName = new DirectoryInfo(tempDir).Name;
        var csProjFile = $"{tempDir}/{projectDirName}.{extension}";

        File.WriteAllText(csProjFile, projectFileContents);
    }

    private static readonly string versionPattern = @"bundleVersion:\s*([^\r\n]+)";
    public static void CreateUnityProject(string tempDir, string version = "1.0.0")
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
    }
}
