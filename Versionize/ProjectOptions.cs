#nullable enable
using LibGit2Sharp;
using NuGet.Versioning;

namespace Versionize;

public record ProjectOptions
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

    public ChangelogOptions Changelog { get; set; } = new ();

    public string GetTagPrefix()
    {
        return TagTemplate
            .Replace("{name}", Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", "", StringComparison.OrdinalIgnoreCase);
    }

    public string GetTagName(string version)
    {
        return GetTagName(SemanticVersion.Parse(version));
    }

    public string GetTagName(SemanticVersion version)
    {
        return TagTemplate
            .Replace("{name}", Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", version.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public SemanticVersion? ExtractTagVersion(Tag tag)
    {
        if (tag.FriendlyName != null)
        {
            var prefix = GetTagPrefix();

            if (tag.FriendlyName != null &&
                tag.FriendlyName.StartsWith(prefix) &&
                SemanticVersion.TryParse(tag.FriendlyName[prefix.Length..], out var version))
            {
                return version;
            }
        }

        return null;
    }
}
