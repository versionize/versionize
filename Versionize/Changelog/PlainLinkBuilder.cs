using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public class PlainLinkBuilder : IChangelogLinkBuilder
{
    public string BuildCommitLink(ConventionalCommit commit)
    {
        return string.Empty;
    }

    public string BuildVersionTagLink(Version newVersion, Version previousVersion, string urlFormat)
    {
        return string.Empty;
    }
}
