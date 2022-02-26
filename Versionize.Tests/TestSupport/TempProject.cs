using System.Xml;

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
}
