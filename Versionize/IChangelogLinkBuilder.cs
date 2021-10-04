using Versionize;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize
{
    public interface IChangelogLinkBuilder
    {
        string BuildCommitLink(ConventionalCommit commit);

        string BuildVersionTagLink(Version version);
    }
}
