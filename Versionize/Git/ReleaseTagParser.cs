using System.Text.RegularExpressions;

namespace Versionize.Git;

/// <summary>
/// Used to parse semantic version from git tags.
/// </summary>
public static partial class ReleaseTagParser
{
    public static string ExtractVersion(string input)
    {
        var match = SemanticVersionRegex().Match(input);
        return match.Success ? match.Groups["version"].Value : "";
    }

    // Matches semantic version strings (e.g., 1.2.3, 1.2.3-alpha.1, 1.2.3+build) with optional prefix characters.
    // Uses named groups for major, minor, patch, prerelease, and buildmetadata components.
    [GeneratedRegex(@"(?:^|[^\d.])(?<version>(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z\-.]+))?(?:\+(?<buildmetadata>[0-9A-Za-z\-.]+))?)")]
    private static partial Regex SemanticVersionRegex();
}
