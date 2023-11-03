using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Versionize;

public static class ConventionalCommitParser
{
    private static readonly string[] NoteKeywords = new string[] { "BREAKING CHANGE" };

    private static readonly Regex HeaderPattern = new("^(?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex IssuesPattern = new("(?<issueToken>#(?<issueId>\\d+))", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    public static List<ConventionalCommit> Parse(List<Commit> commits)
    {
        return commits.ConvertAll(Parse);
    }

    public static ConventionalCommit Parse(Commit commit)
    {
        var conventionalCommit = new ConventionalCommit
        {
            Sha = commit.Sha
        };

        var commitMessageLines = commit.Message.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            )
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var header = commitMessageLines.FirstOrDefault();

        if (header == null)
        {
            return conventionalCommit;
        }

        var match = HeaderPattern.Match(header);
        if (match.Success)
        {
            conventionalCommit.Scope = match.Groups["scope"].Value;
            conventionalCommit.Type = match.Groups["type"].Value;
            conventionalCommit.Subject = match.Groups["subject"].Value;

            if (match.Groups["breakingChangeMarker"].Success)
            {
                conventionalCommit.Notes.Add(new ConventionalCommitNote
                {
                    Title = "BREAKING CHANGE",
                    Text = string.Empty
                });
            }

            var issuesMatch = IssuesPattern.Matches(conventionalCommit.Subject);
            foreach (var issueMatch in issuesMatch.Cast<Match>())
            {
                conventionalCommit.Issues.Add(
                    new ConventionalCommitIssue
                    {
                        Token = issueMatch.Groups["issueToken"].Value,
                        Id = issueMatch.Groups["issueId"].Value,
                    });
            }
        }
        else
        {
            conventionalCommit.Subject = header;
        }

        for (var i = 1; i < commitMessageLines.Count; i++)
        {
            foreach (var noteKeyword in NoteKeywords)
            {
                var line = commitMessageLines[i];
                if (line.StartsWith($"{noteKeyword}:"))
                {
                    conventionalCommit.Notes.Add(new ConventionalCommitNote
                    {
                        Title = noteKeyword,
                        Text = line[$"{noteKeyword}:".Length..].TrimStart()
                    });
                }
            }
        }

        return conventionalCommit;
    }
}
