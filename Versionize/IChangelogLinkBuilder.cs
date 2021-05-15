using Versionize;
using Version = NuGet.Versioning.SemanticVersion;

public interface IChangelogLinkBuilder
{
    string BuildCommitLink(ConventionalCommit commit);

    string BuildVersionTagLink(Version version);
}
