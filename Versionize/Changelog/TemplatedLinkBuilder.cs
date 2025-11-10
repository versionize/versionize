using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Git;

namespace Versionize.Changelog;

public sealed class TemplatedLinkBuilder(ChangelogLinkTemplates templates, IChangelogLinkBuilder fallbackBuilder) : IChangelogLinkBuilder
{
    private readonly ChangelogLinkTemplates _templates = templates;
    private readonly IChangelogLinkBuilder _fallbackBuilder = fallbackBuilder;

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

    public string BuildVersionTagLink(string currentTag, string previousTag)
    {
        if (_templates.VersionTagLink is { } template)
        {
            // NOTE: Backward compatibility - only extract version if template contains {version}
            var version = template.Contains("{version}", StringComparison.OrdinalIgnoreCase)
                ? ReleaseTagParser.ExtractVersion(currentTag)
                : "";

            return template
                .Replace("{version}", version, StringComparison.OrdinalIgnoreCase)
                .Replace("{currentTag}", currentTag, StringComparison.OrdinalIgnoreCase)
                .Replace("{previousTag}", previousTag, StringComparison.OrdinalIgnoreCase);
        }

        return _fallbackBuilder.BuildVersionTagLink(currentTag, previousTag);
    }
}
