using System.Text.RegularExpressions;
using NuGet.Versioning;
using Versionize.CommandLine;

namespace Versionize.BumpFiles;

/// <summary>
/// Generic bump file implementation that extracts and updates a semantic version
/// using a user supplied regular expression with a single capturing group for the version.
/// This enables version bumps in arbitrary text based file formats (yaml, json, txt, etc.).
/// </summary>
public sealed class RegexBumpFile(string filePath, string versionPattern, SemanticVersion version) : IBumpFile
{
    private readonly string _filePath = filePath;
    private readonly string _versionPattern = versionPattern;

    public SemanticVersion Version { get; } = version;

    /// <summary>
    /// Creates a <see cref="RegexBumpFile"/> by reading the file and parsing the version using the provided pattern.
    /// The pattern MUST contain exactly one capturing group which returns the version string.
    /// </summary>
    public static RegexBumpFile Create(string filePath, string versionPattern)
    {
        var version = GetVersion(filePath, versionPattern);
        return new RegexBumpFile(filePath, versionPattern, version);
    }

    public void WriteVersion(SemanticVersion newVersion)
    {
        if (!File.Exists(_filePath))
        {
            throw new VersionizeException($"Version file '{_filePath}' not found", 1);
        }

        string content = File.ReadAllText(_filePath);
        // Replace only first occurrence to avoid unintended replacements.
        var regex = new Regex(_versionPattern, RegexOptions.Multiline);
        string updated = regex.Replace(content, match =>
        {
            if (match.Groups.Count < 2)
            {
                return match.Value; // No group to replace; leave unchanged.
            }

            var versionGroup = match.Groups[1];
            var relativeIndex = versionGroup.Index - match.Index;
            var before = match.Value.Substring(0, relativeIndex);
            var after = match.Value.Substring(relativeIndex + versionGroup.Length);
            return before + newVersion + after;
        }, 1); // replace only first occurrence

        File.WriteAllText(_filePath, updated);
    }

    public IEnumerable<string> GetFilePaths() => [_filePath];

    private static SemanticVersion GetVersion(string filePath, string versionPattern)
    {
        if (!File.Exists(filePath))
        {
            throw new VersionizeException($"Version file '{filePath}' not found", 1);
        }

        string content = File.ReadAllText(filePath);
        var match = Regex.Match(content, versionPattern, RegexOptions.Multiline);
        if (match.Success && match.Groups.Count >= 2)
        {
            var versionString = match.Groups[1].Value.Trim();
            try
            {
                return SemanticVersion.Parse(versionString);
            }
            catch (Exception)
            {
                throw new VersionizeException($"Version '{versionString}' parsed from '{filePath}' is not a valid semantic version", 1);
            }
        }

        throw new VersionizeException($"Version could not be parsed from '{filePath}' using pattern '{versionPattern}'", 1);
    }
}
