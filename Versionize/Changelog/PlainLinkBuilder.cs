#nullable enable
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public class PlainLinkBuilder : IChangelogLinkBuilder
{
    private readonly PlainLinkTemplates? _templates;

    public PlainLinkBuilder(PlainLinkTemplates? templates = null)
    {
        _templates = templates;
    }

    public string BuildIssueLink(string issueId)
    {
        if (_templates?.IssueLink is { } template)
        {
            return template.Replace(
                "{issue}", issueId, StringComparison.OrdinalIgnoreCase);
        }

        return string.Empty;
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        if (_templates?.CommitLink is { } template)
        {
            return template.Replace(
                "{commitSha}", commit.Sha, StringComparison.OrdinalIgnoreCase);
        }

        return string.Empty;
    }

    public string BuildVersionTagLink(Version version)
    {
        if (_templates?.VersionTagLink is { } template)
        {
            return template.Replace(
                "{version}", version.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return string.Empty;
    }
}
