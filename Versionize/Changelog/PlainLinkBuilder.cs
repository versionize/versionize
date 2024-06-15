#nullable enable
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public class PlainLinkBuilder : IChangelogLinkBuilder
{
    public string BuildIssueLink(string issueId) => string.Empty;

    public string BuildCommitLink(ConventionalCommit commit) => string.Empty;

    public string BuildVersionTagLink(Version version) => string.Empty;
}
