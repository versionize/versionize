namespace Versionize.Config;

public sealed record class ChangelogOptions
{
    public const string Preamble = "# Change Log\n\nAll notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.\n";
    public static readonly ChangelogOptions Default = new()
    {
        Header = Preamble,
        IncludeAllCommits = false,
        Sections =
        [
            new ChangelogSection { Type = "feat", Section = "Features", Hidden = false },
            new ChangelogSection { Type = "fix", Section = "Bug Fixes", Hidden = false },
        ],
        Path = String.Empty
    };

    public string Header { get; set; }
    public string Path { get; set; }
    public bool? IncludeAllCommits { get; set; }
    public IEnumerable<ChangelogSection> Sections { get; set; }
    public ChangelogLinkTemplates LinkTemplates { get; set; }

    public static ChangelogOptions Merge(ChangelogOptions customOptions, ChangelogOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new ChangelogOptions
        {
            Header = customOptions.Header ?? defaultOptions.Header,
            IncludeAllCommits = customOptions.IncludeAllCommits ?? defaultOptions.IncludeAllCommits,
            Sections = customOptions.Sections ?? defaultOptions.Sections,
            LinkTemplates = customOptions.LinkTemplates ?? defaultOptions.LinkTemplates,
            Path = customOptions.Path ?? defaultOptions.Path,
        };
    }
}

public record ChangelogLinkTemplates
{
    public string IssueLink { get; set; }

    public string CommitLink { get; set; }

    public string VersionTagLink { get; set; }
}
