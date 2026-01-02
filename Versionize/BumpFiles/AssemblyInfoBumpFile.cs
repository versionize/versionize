using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace Versionize.BumpFiles;

/// <summary>
/// Handles version bumping in AssemblyInfo.cs files.
/// </summary>
public sealed class AssemblyInfoBumpFile
{
    private readonly string _filePath;
    private readonly string _versionElement;

    private AssemblyInfoBumpFile(string filePath, string versionElement)
    {
        _filePath = filePath;
        _versionElement = versionElement;
    }

    public static AssemblyInfoBumpFile? TryCreate(string projectDirectory, string versionElement)
    {
        var assemblyInfoPath = Path.Combine(projectDirectory, "Properties", "AssemblyInfo.cs");
        
        if (!File.Exists(assemblyInfoPath))
        {
            return null;
        }

        if (!IsVersionable(assemblyInfoPath, versionElement))
        {
            return null;
        }

        return new AssemblyInfoBumpFile(assemblyInfoPath, versionElement);
    }

    public void WriteVersion(SemanticVersion version)
    {
        var content = File.ReadAllText(_filePath);
        var versionString = $"{version.Major}.{version.Minor}.{version.Patch}.0";

        if (_versionElement == "Version")
        {
            content = UpdateAttribute(content, "AssemblyVersion", versionString);
            content = UpdateAttribute(content, "AssemblyFileVersion", versionString);
        }
        else
        {
            content = UpdateAttribute(content, _versionElement, versionString);
        }

        File.WriteAllText(_filePath, content);
    }

    public string FilePath => _filePath;

    private static bool IsVersionable(string filePath, string versionElement)
    {
        var content = File.ReadAllText(filePath);

        if (versionElement == "Version")
        {
            return Regex.IsMatch(content, @"\[assembly:\s*(AssemblyVersion|AssemblyFileVersion)\s*\(");
        }

        var pattern = @$"\[assembly:\s*{Regex.Escape(versionElement)}\s*\(";
        return Regex.IsMatch(content, pattern);
    }

    private static string UpdateAttribute(string content, string attributeName, string version)
    {
        var pattern = @$"\[assembly:\s*{Regex.Escape(attributeName)}\s*\(\s*""[^""]*""\s*\)\s*\]";
        var replacement = $"[assembly: {attributeName}(\"{version}\")]";
        return Regex.Replace(content, pattern, replacement);
    }
}
