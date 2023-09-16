using System.Text;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public class ChangelogBuilder
{
    private ChangelogBuilder(string file)
    {
        FilePath = file;
    }

    public string FilePath { get; }

    public void Write(
        Version version,
        DateTimeOffset versionTime,
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ChangelogOptions changelogOptions)
    {
        string markdown = GenerateMarkdown(version, versionTime, linkBuilder, commits, changelogOptions);

        if (File.Exists(FilePath))
        {
            var contents = File.ReadAllText(FilePath);

            var firstReleaseHeadlineIdx = contents.IndexOf("<a name=\"", StringComparison.Ordinal);

            if (firstReleaseHeadlineIdx >= 0)
            {
                markdown = contents.Insert(firstReleaseHeadlineIdx, markdown);
            }
            else
            {
                markdown = contents + "\n\n" + markdown;
            }

            File.WriteAllText(FilePath, markdown);
        }
        else
        {
            File.WriteAllText(FilePath, changelogOptions.Header + "\n" + markdown);
        }
    }

    public static string GenerateMarkdown(
        Version version,
        DateTimeOffset versionTime,
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ChangelogOptions changelogOptions)
    {
        var versionTagLink = string.IsNullOrWhiteSpace(linkBuilder.BuildVersionTagLink(version))
            ? version.ToString()
            : $"[{version}]({linkBuilder.BuildVersionTagLink(version)})";

        var markdown = $"<a name=\"{version}\"></a>";
        markdown += "\n";
        markdown += $"## {versionTagLink} ({versionTime.ToString("yyyy-MM-dd")})";
        markdown += "\n";
        markdown += "\n";

        var visibleChangelogSections = changelogOptions.Sections.Where(x => !x.Hidden);

        foreach (var changelogSection in visibleChangelogSections)
        {
            var matchingCommits = commits.Where(commit => commit.Type == changelogSection.Type);
            var buildBlock = BuildBlock(changelogSection.Section, linkBuilder, matchingCommits);
            if (!string.IsNullOrWhiteSpace(buildBlock))
            {
                markdown += buildBlock;
                markdown += "\n";
            }
        }

        var breaking = BuildBlock("Breaking Changes", linkBuilder, commits.Where(commit => commit.IsBreakingChange));

        if (!string.IsNullOrWhiteSpace(breaking))
        {
            markdown += breaking;
            markdown += "\n";
        }

        if (changelogOptions.IncludeAllCommits)
        {
            var other = BuildBlock(
                "Other",
                linkBuilder,
                commits.Where(commit => !visibleChangelogSections.Any(x => x.Type == commit.Type) && !commit.IsBreakingChange));

            if (!string.IsNullOrWhiteSpace(other))
            {
                markdown += other;
                markdown += "\n";
            }
        }

        return markdown;
    }

    public static string BuildBlock(string header, IChangelogLinkBuilder linkBuilder, IEnumerable<ConventionalCommit> commits)
    {
        if (!commits.Any())
        {
            return null;
        }

        var block = $"### {header}";
        block += "\n";
        block += "\n";

        return commits
            .OrderBy(c => c.Scope)
            .ThenBy(c => c.Subject)
            .Aggregate(block, (current, commit) => current + BuildCommit(commit, linkBuilder) + "\n");
    }

    public static string BuildCommit(ConventionalCommit commit, IChangelogLinkBuilder linkBuilder)
    {
        var sb = new StringBuilder("* ");

        if (!string.IsNullOrWhiteSpace(commit.Scope))
        {
            sb.Append($"**{commit.Scope}:** ");
        }

        sb.Append(commit.Subject);

        var commitLink = linkBuilder.BuildCommitLink(commit);

        if (!string.IsNullOrWhiteSpace(commitLink))
        {
            sb.Append($" ([{commit.Sha.Substring(0, 7)}]({commitLink}))");
        }

        return sb.ToString();
    }

    public static ChangelogBuilder CreateForPath(string directory)
    {
        var changelogFile = Path.Combine(directory, "CHANGELOG.md");

        return new ChangelogBuilder(changelogFile);
    }
}
