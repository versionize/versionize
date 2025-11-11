using LibGit2Sharp;
using NuGet.Versioning;

namespace Versionize.Config;

public sealed record ProjectOptions
{
    public static readonly ProjectOptions DefaultOneProjectPerRepo =
        new()
        {
            Name = "",
            Path = "",
            TagTemplate = "v{version}",
            Changelog = ChangelogOptions.Default
        };

    public string Name { get; init; } = "";

    public string Path { get; init; } = "";

    public string TagTemplate { get; init; } = "{name}/v{version}";

    /// <summary>
    /// Specifies the .NET XML property to use when reading or writing the project version.
    /// 'Version' by default.<br />
    /// Only alphanumeric and underscore characters are allowed.
    /// </summary>
    /// <example>
    /// <PropertyGroup>
    ///   <FileVersion>1.0.0</FileVersion>
    /// </PropertyGroup>
    /// </example>
    public string? VersionElement { get; init; }

    public ChangelogOptions Changelog { get; init; } = new();

    public string GetTagName(SemanticVersion version)
    {
        return TagTemplate
            .Replace("{name}", Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", version.ToFullString(), StringComparison.OrdinalIgnoreCase);
    }

    public SemanticVersion? ExtractTagVersion(Tag tag)
    {
        if (string.IsNullOrEmpty(tag.FriendlyName))
        {
            return null;
        }

        // Split the template into prefix and suffix around {version}
        var versionPlaceholder = "{version}";
        var templateWithName = TagTemplate.Replace("{name}", Name, StringComparison.OrdinalIgnoreCase);
        var versionIndex = templateWithName.IndexOf(versionPlaceholder, StringComparison.OrdinalIgnoreCase);

        if (versionIndex == -1)
        {
            return null;
        }

        var prefix = templateWithName[..versionIndex];
        var suffix = templateWithName[(versionIndex + versionPlaceholder.Length)..];

        // Check if tag matches the prefix and suffix pattern
        if (!tag.FriendlyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!tag.FriendlyName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Extract the version string between prefix and suffix
        var versionStartIndex = prefix.Length;
        var versionLength = tag.FriendlyName.Length - prefix.Length - suffix.Length;

        if (versionLength <= 0)
        {
            return null;
        }

        var versionString = tag.FriendlyName.Substring(versionStartIndex, versionLength);

        if (SemanticVersion.TryParse(versionString, out var version))
        {
            return version;
        }

        return null;
    }
}
