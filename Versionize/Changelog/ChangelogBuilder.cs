using System.Text;
using Versionize.ConventionalCommits;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.Config;

namespace Versionize.Changelog;

public sealed class ChangelogBuilder
{
    private ChangelogBuilder(string file)
    {
        FilePath = file;
    }

    public string FilePath { get; }

    public static ChangelogBuilder CreateForPath(string directory)
    {
        var changelogFile = Path.Combine(directory, "CHANGELOG.md");

        return new ChangelogBuilder(changelogFile);
    }

    public void Write(
        Version newVersion,
        Version previousVersion,
        DateTimeOffset versionTime,
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ProjectOptions projectOptions,
        IReadOnlyDictionary<string, string[]>? aliases = null)
    {
        string markdown = GenerateMarkdown(newVersion, previousVersion, versionTime, linkBuilder, commits, projectOptions, aliases);

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
            File.WriteAllText(FilePath, projectOptions.Changelog.Header + "\n" + markdown);
        }
    }

    public static string GenerateMarkdown(
        Version newVersion,
        Version previousVersion,
        DateTimeOffset versionTime,
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ProjectOptions projectOptions,
        IReadOnlyDictionary<string, string[]>? aliases = null)
    {
        var currentTag = projectOptions.GetTagName(newVersion);
        var previousTag = projectOptions.GetTagName(previousVersion);
        var compareUrl = linkBuilder.BuildVersionTagLink(currentTag, previousTag);
        var versionTagLink = string.IsNullOrWhiteSpace(compareUrl)
            ? newVersion.ToString()
            : $"[{newVersion}]({compareUrl})";

        var markdown = $"<a name=\"{newVersion}\"></a>";
        markdown += "\n";
        markdown += $"## {versionTagLink} ({versionTime:yyyy-MM-dd})";
        markdown += "\n";
        markdown += "\n";

        return markdown + GenerateCommitList(linkBuilder, commits, projectOptions.Changelog, aliases);
    }

    public static string GenerateCommitList(
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ChangelogOptions changelogOptions,
        IReadOnlyDictionary<string, string[]>? aliases = null)
    {
        var markdown = "";

        var aliasLookup = aliases?.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant(),
            kvp => new HashSet<string>(kvp.Value.Select(v => v.ToLowerInvariant())))
            ?? new Dictionary<string, HashSet<string>>();

        bool MatchesSection(ConventionalCommit commit, string sectionType)
        {
            if (string.IsNullOrWhiteSpace(commit.Type)) return false;
            var commitTypeLower = commit.Type.ToLowerInvariant();
            var sectionLower = sectionType.ToLowerInvariant();
            if (commitTypeLower == sectionLower) return true;
            if (aliasLookup.TryGetValue(sectionLower, out var set)) return set.Contains(commitTypeLower);
            return false;
        }

        var visibleChangelogSections = changelogOptions.Sections is null
            ? []
            : changelogOptions.Sections.Where(x => !x.Hidden);

        foreach (var changelogSection in visibleChangelogSections)
        {
            var matchingCommits = commits.Where(commit => MatchesSection(commit, changelogSection.Type));
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

        if (changelogOptions.IncludeAllCommits.GetValueOrDefault())
        {
            var other = BuildBlock(
                changelogOptions.OtherSection ?? "Other",
                linkBuilder,
                commits.Where(commit => !visibleChangelogSections.Any(x => MatchesSection(commit, x.Type)) && !commit.IsBreakingChange));

            if (!string.IsNullOrWhiteSpace(other))
            {
                markdown += other;
                markdown += "\n";
            }
        }

        return markdown;
    }

    private static string? BuildBlock(string? header, IChangelogLinkBuilder linkBuilder, IEnumerable<ConventionalCommit> commits)
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

    private static string BuildCommit(ConventionalCommit commit, IChangelogLinkBuilder linkBuilder)
    {
        var sb = new StringBuilder("* ");

        if (!string.IsNullOrWhiteSpace(commit.Scope))
        {
            sb.Append($"**{commit.Scope}:** ");
        }

        var subject = commit.Subject;
        foreach (var issue in commit.Issues)
        {
            if (string.IsNullOrEmpty(subject))
            {
                continue;
            }
            if (string.IsNullOrEmpty(issue.Id))
            {
                continue;
            }
            if (string.IsNullOrEmpty(issue.Token))
            {
                continue;
            }

            var issueLink = linkBuilder.BuildIssueLink(issue.Id);
            if (!string.IsNullOrEmpty(issueLink))
            {
                subject = subject.Replace(issue.Token, $"[{issue.Token}]({issueLink})");
            }
        }

        sb.Append(subject);

        var commitLink = linkBuilder.BuildCommitLink(commit);

        if (!string.IsNullOrWhiteSpace(commitLink))
        {
            var shortSha = commit.Sha?[..7];
            sb.Append($" ([{shortSha}]({commitLink}))");
        }

        return sb.ToString();
    }
}
