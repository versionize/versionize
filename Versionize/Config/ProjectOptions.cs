using System.Text.RegularExpressions;
using LibGit2Sharp;
using NuGet.Versioning;

namespace Versionize.Config;

public sealed partial record ProjectOptions
{
    [GeneratedRegex(@"v?(\d+\.\d+\.\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionWithoutPrefixGeneratedRegex();

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

    public bool OmitTagVersionPrefix { get; set; }

    public bool HasMatchingTag(SemanticVersion version, string tagName)
    {
        return TagNameWithoutVersionPrefix(GetTagName(version))
            .Equals(TagNameWithoutVersionPrefix(tagName), StringComparison.OrdinalIgnoreCase);
    }

    public string GetTagPrefix()
    {
        return StripVersionPrefixIfConfigured()
               .Replace("{name}", Name, StringComparison.OrdinalIgnoreCase)
               .Replace("{version}", "", StringComparison.OrdinalIgnoreCase);
    }

    public string GetTagName(string version)
    {
        return GetTagName(SemanticVersion.Parse(version));
    }

    public string GetTagName(SemanticVersion version)
    {
        return StripVersionPrefixIfConfigured()
               .Replace("{name}", Name, StringComparison.OrdinalIgnoreCase)
               .Replace("{version}", version.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public SemanticVersion? ExtractTagVersion(Tag tag)
    {
        if (tag.FriendlyName != null)
        {
            var prefix = GetTagPrefix();
            if (OmitTagVersionPrefix && tag.FriendlyName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "v" + prefix;
            }
            else if (!OmitTagVersionPrefix && !tag.FriendlyName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                prefix = prefix.TrimStart('v');
            }

            if (tag.FriendlyName != null &&
                tag.FriendlyName.StartsWith(prefix) &&
                SemanticVersion.TryParse(tag.FriendlyName[prefix.Length..], out var version))
            {
                return version;
            }
        }

        return null;
    }

    private string StripVersionPrefixIfConfigured()
    {
        return OmitTagVersionPrefix
            ? TagTemplate.Replace("v{version}", "{version}")
            : TagTemplate;
    }

    private static string TagNameWithoutVersionPrefix(string tagName)
    {
        return VersionWithoutPrefixGeneratedRegex().Replace(tagName, "$1");
    }
}
