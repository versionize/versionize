using Versionize.ConventionalCommits;

namespace Versionize.Changelog;

public interface IChangelogLinkBuilder
{
    string BuildIssueLink(string issueId);

    string BuildCommitLink(ConventionalCommit commit);

    string BuildVersionTagLink(string currentTag, string previousTag);
}
