using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace Versionize.BumpFiles;

public sealed class UnityBumpFile(string projectSettingsPath, SemanticVersion version) : IBumpFile
{
    private static readonly string versionPattern = @"bundleVersion:\s*([^\r\n]+)";

    private readonly string _projectSettingsPath = projectSettingsPath;
    private readonly SemanticVersion _version = version;

    public SemanticVersion Version => _version;

    public static UnityBumpFile Create(string workingDirectory)
    {
        var projectSettingsPath = Path.Combine(workingDirectory, "ProjectSettings/ProjectSettings.asset");
        var version = GetVersion(projectSettingsPath);
        return new UnityBumpFile(projectSettingsPath, version);
    }

    public static SemanticVersion GetVersion(string projectSettingsPath)
    {
        if (!File.Exists(projectSettingsPath))
        {
            throw new FileNotFoundException("ProjectSettings.asset not found");
        }

        string projectSettings = File.ReadAllText(projectSettingsPath);
        var match = Regex.Match(projectSettings, versionPattern);
        if (match.Success)
        {
            var versionString = match.Groups[1].Value.Trim();
            // TODO: Consider catching exception
            return SemanticVersion.Parse(versionString);
        }

        throw new FileNotFoundException("Version could not be parsed from ProjectSettings.asset");
    }

    public void WriteVersion(SemanticVersion newVersion)
    {
        if (!File.Exists(_projectSettingsPath))
        {
            throw new FileNotFoundException("ProjectSettings.asset not found.");
        }

        string projectSettings = File.ReadAllText(_projectSettingsPath);
        string updatedSettings = Regex.Replace(projectSettings, versionPattern, $"bundleVersion: {newVersion}");
        File.WriteAllText(_projectSettingsPath, updatedSettings);
    }

    public IEnumerable<string> GetFilePaths()
    {
        return [_projectSettingsPath];
    }
}
