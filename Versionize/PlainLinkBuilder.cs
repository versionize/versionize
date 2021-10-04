using Versionize;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize
{
    public class PlainLinkBuilder : IChangelogLinkBuilder
    {
        public string BuildCommitLink(ConventionalCommit commit)
        {
            return string.Empty;
        }

        public string BuildVersionTagLink(Version version)
        {
            return string.Empty;
        }
    }
}
