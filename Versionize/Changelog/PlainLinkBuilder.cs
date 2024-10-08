using Versionize.ConventionalCommits;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public sealed class PlainLinkBuilder : IChangelogLinkBuilder
{
    public string BuildIssueLink(string issueId) => string.Empty;

    public string BuildCommitLink(ConventionalCommit commit) => string.Empty;

    public string BuildVersionTagLink(Version version) => string.Empty;
}
