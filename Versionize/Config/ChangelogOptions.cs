namespace Versionize.Config;

public sealed record class ChangelogOptions
{
    public const string Preamble = "# Change Log\n\nAll notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.\n";
    public static readonly ChangelogOptions Default = new()
    {
        Header = Preamble,
        Path = string.Empty,
        IncludeAllCommits = false,
        OtherSection = "Other",
        Sections =
        [
            new ChangelogSection { Type = "feat", Section = "Features", Hidden = false },
            new ChangelogSection { Type = "fix", Section = "Bug Fixes", Hidden = false },
        ],
    };

    public string? Header { get; init; }
    public string? Path { get; init; }
    public bool? IncludeAllCommits { get; init; }
    public string? OtherSection { get; init; }
    public IEnumerable<ChangelogSection>? Sections { get; init; }
    public ChangelogLinkTemplates? LinkTemplates { get; init; }

    public static ChangelogOptions Merge(ChangelogOptions? customOptions, ChangelogOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new ChangelogOptions
        {
            Header = customOptions.Header ?? defaultOptions.Header,
            Path = customOptions.Path ?? defaultOptions.Path,
            IncludeAllCommits = customOptions.IncludeAllCommits ?? defaultOptions.IncludeAllCommits,
            OtherSection = customOptions.OtherSection ?? defaultOptions.OtherSection,
            Sections = customOptions.Sections ?? defaultOptions.Sections,
            LinkTemplates = customOptions.LinkTemplates ?? defaultOptions.LinkTemplates,
        };
    }
}

public record ChangelogLinkTemplates
{
    public string? IssueLink { get; init; }

    public string? CommitLink { get; init; }

    public string? VersionTagLink { get; init; }
}
