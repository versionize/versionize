using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public interface IChangelogLinkBuilder
{
    string BuildIssueLink(string issueId);

    string BuildCommitLink(ConventionalCommit commit);

    string BuildVersionTagLink(Version version);
}
