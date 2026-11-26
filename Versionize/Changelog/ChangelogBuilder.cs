using System.Text;
using Versionize.ConventionalCommits;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.Config;
using Versionize.Changelog.LinkBuilders;

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
        ProjectOptions projectOptions)
    {
        string markdown = GenerateMarkdown(newVersion, previousVersion, versionTime, linkBuilder, commits, projectOptions);

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
        ProjectOptions projectOptions)
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

        return markdown + GenerateCommitList(linkBuilder, commits, projectOptions.Changelog);
    }

    public static string GenerateCommitList(
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ChangelogOptions changelogOptions)
    {
        var markdown = "";

        // Skip release commits created by automation: type = "chore" and scope = "release"
        var filteredCommits = commits.Where(c => !(
            string.Equals(c.Type, "chore", StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.Scope, "release", StringComparison.OrdinalIgnoreCase)));

        var visibleChangelogSections = changelogOptions.Sections is null
            ? []
            : changelogOptions.Sections.Where(x => !x.Hidden);

        foreach (var changelogSection in visibleChangelogSections)
        {
            var matchingCommits = filteredCommits.Where(commit => commit.Type == changelogSection.Type);
            var buildBlock = BuildBlock(changelogSection.Section, linkBuilder, matchingCommits);
            if (!string.IsNullOrWhiteSpace(buildBlock))
            {
                markdown += buildBlock;
                markdown += "\n";
            }
        }

        var breaking = BuildBlock("Breaking Changes", linkBuilder, filteredCommits.Where(commit => commit.IsBreakingChange));

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
                filteredCommits.Where(commit => !visibleChangelogSections.Any(x => x.Type == commit.Type) && !commit.IsBreakingChange));

            if (!string.IsNullOrWhiteSpace(other))
            {
                markdown += other;
                markdown += "\n";
            }
        }

        if (changelogOptions.IncludeAuthors.GetValueOrDefault())
        {
            var authors = BuildAuthorsBlock(
                changelogOptions.AuthorsSection ?? "Thank You",
                filteredCommits,
                linkBuilder);

            if (!string.IsNullOrWhiteSpace(authors))
            {
                markdown += authors;
                markdown += "\n";
            }
        }

        return markdown;
    }

    private static string? BuildAuthorsBlock(string header, IEnumerable<ConventionalCommit> commits, IChangelogLinkBuilder linkBuilder)
    {
        var resolver = linkBuilder as IUsernameResolver;

        var uniqueAuthors = commits
            .Where(c => !string.IsNullOrWhiteSpace(c.AuthorName))
            .GroupBy(c => c.AuthorEmail ?? c.AuthorName!)
            .Select(g => new { Name = g.First().AuthorName!, Sha = g.First().Sha })
            .OrderBy(a => a.Name)
            .ToList();

        if (uniqueAuthors.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.Append($"### {header}");
        sb.Append("\n");
        sb.Append("\n");

        foreach (var author in uniqueAuthors)
        {
            string? username = resolver != null && !string.IsNullOrEmpty(author.Sha)
                ? resolver.ResolveUsername(author.Sha)
                : null;

            sb.Append(string.IsNullOrWhiteSpace(username)
                ? $"* {author.Name}\n"
                : $"* {author.Name} @{username}\n");
        }

        return sb.ToString();
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
        if (string.IsNullOrEmpty(subject))
        {
            subject = string.Empty;
        }

        var references = new List<(int Index, int Length, string Link)>();
        foreach (var pr in commit.PullRequests)
        {
            if (string.IsNullOrEmpty(pr.Id) || string.IsNullOrEmpty(pr.Token))
            {
                continue;
            }

            var prLink = linkBuilder.BuildPullRequestLink(pr.Id);
            if (!string.IsNullOrEmpty(prLink))
            {
                references.Add((pr.Index, pr.Length, $"[{pr.Token}]({prLink})"));
            }
        }

        foreach (var issue in commit.Issues)
        {
            if (string.IsNullOrEmpty(issue.Id) || string.IsNullOrEmpty(issue.Token))
            {
                continue;
            }

            var issueLink = linkBuilder.BuildIssueLink(issue.Id);
            if (!string.IsNullOrEmpty(issueLink))
            {
                references.Add((issue.Index, issue.Length, $"[{issue.Token}]({issueLink})"));
            }
        }

        var renderedSubject = new StringBuilder();
        var position = 0;
        foreach (var reference in references
                     .OrderBy(reference => reference.Index)
                     .ThenByDescending(reference => reference.Length))
        {
            if (reference.Index < position
                || reference.Index < 0
                || reference.Length <= 0
                || reference.Index + reference.Length > subject.Length)
            {
                continue;
            }

            renderedSubject.Append(subject, position, reference.Index - position);
            renderedSubject.Append(reference.Link);
            position = reference.Index + reference.Length;
        }

        renderedSubject.Append(subject, position, subject.Length - position);
        sb.Append(renderedSubject);

        var commitLink = linkBuilder.BuildCommitLink(commit);

        if (!string.IsNullOrWhiteSpace(commitLink))
        {
            var shortSha = commit.Sha?[..7];
            sb.Append($" ([{shortSha}]({commitLink}))");
        }

        return sb.ToString();
    }
}
