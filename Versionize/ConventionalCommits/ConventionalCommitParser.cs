using System.Text.RegularExpressions;
using LibGit2Sharp;
using Versionize.Config;

namespace Versionize.ConventionalCommits;

public static class ConventionalCommitParser
{
    private static readonly string[] NoteKeywords = ["BREAKING CHANGE"];

    private const string DefaultHeaderPattern = "^(?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$";

    private const string DefaultIssuesPattern = "(?<issueToken>#(?<issueId>\\d+))";

    private static readonly RegexOptions RegexOptions =
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline;

    public static List<ConventionalCommit> Parse(List<Commit> commits, CommitParserOptions? options = null)
    {
        return [.. commits.Select(x => Parse(x, options))];
    }

    public static ConventionalCommit Parse(Commit commit)
    {
        return Parse(commit, null);
    }

    public static ConventionalCommit Parse(Commit commit, CommitParserOptions? options)
    {
        var conventionalCommit = new ConventionalCommit
        {
            Sha = commit.Sha
        };

        var commitMessageLines = commit.Message
            .Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var header = commitMessageLines.FirstOrDefault();

        if (header == null)
        {
            return conventionalCommit;
        }

        var headerPatterns = new List<string>(options?.HeaderPatterns ?? [])
        {
            DefaultHeaderPattern
        };

        var issuesPatterns = new List<string>(options?.IssuesPatterns ?? [])
        {
            DefaultIssuesPattern
        };

        Match? headerMatch = null;
        foreach (var headerPattern in headerPatterns)
        {
            headerMatch = Regex.Match(header, headerPattern, RegexOptions);
            if (headerMatch.Success)
            {
                break;
            }
        }

        if (headerMatch is { Success: true } match)
        {
            conventionalCommit = conventionalCommit with
            {
                Type = match.Groups["type"].Value,
                Scope = match.Groups["scope"].Value,
                Subject = match.Groups["subject"].Value
            };

            if (match.Groups["breakingChangeMarker"].Success)
            {
                conventionalCommit.Notes.Add(new ConventionalCommitNote
                {
                    Title = "BREAKING CHANGE",
                    Text = string.Empty
                });
            }

            foreach (var issuesPattern in issuesPatterns)
            {
                var issuesMatch = Regex.Matches(conventionalCommit.Subject, issuesPattern, RegexOptions);
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
        }
        else
        {
            conventionalCommit = conventionalCommit with
            {
                Subject = header
            };
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
