using Versionize.Config;
using Versionize.ConventionalCommits;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public sealed class TemplatedLinkBuilder : IChangelogLinkBuilder
{
    private readonly ChangelogLinkTemplates _templates;
    private readonly IChangelogLinkBuilder _fallbackBuilder;

    public TemplatedLinkBuilder(ChangelogLinkTemplates templates, IChangelogLinkBuilder fallbackBuilder)
    {
        _templates = templates;
        _fallbackBuilder = fallbackBuilder;
    }

    public string BuildIssueLink(string issueId)
    {
        if (_templates.IssueLink is { } template)
        {
            return template.Replace(
                "{issue}", issueId, StringComparison.OrdinalIgnoreCase);
        }

        return _fallbackBuilder.BuildIssueLink(issueId);
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        if (_templates.CommitLink is { } template)
        {
            return template.Replace(
                "{commitSha}", commit.Sha, StringComparison.OrdinalIgnoreCase);
        }

        return _fallbackBuilder.BuildCommitLink(commit);
    }

    public string BuildVersionTagLink(Version version)
    {
        if (_templates.VersionTagLink is { } template)
        {
            return template.Replace(
                "{version}", version.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return _fallbackBuilder.BuildVersionTagLink(version);
    }
}
