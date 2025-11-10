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

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public string TagTemplate { get; set; } = "{name}/v{version}";

    public ChangelogOptions Changelog { get; set; } = new();

    // TODO: Remove. Only used by a test.
    public string GetTagName(string version)
    {
        return GetTagName(SemanticVersion.Parse(version));
    }

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
