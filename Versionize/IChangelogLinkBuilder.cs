using System;
using Versionize;

public interface IChangelogLinkBuilder
{
    string BuildCommitLink(ConventionalCommit commit);

    string BuildVersionTagLink(Version version);
}
