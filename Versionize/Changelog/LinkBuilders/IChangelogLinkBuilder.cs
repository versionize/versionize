using Versionize.ConventionalCommits;

namespace Versionize.Changelog.LinkBuilders;

public interface IChangelogLinkBuilder
{
    string BuildIssueLink(string issueId);

    string BuildPullRequestLink(string prId) => BuildIssueLink(prId);

    string BuildCommitLink(ConventionalCommit commit);

    string BuildVersionTagLink(string currentTag, string previousTag);
}
