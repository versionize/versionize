using Versionize.ConventionalCommits;

namespace Versionize.Changelog;

public sealed class PlainLinkBuilder : IChangelogLinkBuilder
{
    public string BuildIssueLink(string issueId) => string.Empty;

    public string BuildCommitLink(ConventionalCommit commit) => string.Empty;

    public string BuildVersionTagLink(string currentTag, string previousTag)
    {
        return string.Empty;
    }
}
