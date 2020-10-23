using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Versionize
{
    public class ConventionalCommitParser
    {
        static readonly string[] noteKeywords = new string[] { "BREAKING CHANGE" };

        private static readonly Regex HeaderPattern = new Regex("^(?<type>\\w*)(?:\\((?<scope>.*)\\))?: (?<subject>.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        public ConventionalCommitParser()
        {
        }

        public List<ConventionalCommit> Parse(List<Commit> commits)
        {
            return commits.Select(Parse).ToList();
        }

        public ConventionalCommit Parse(Commit commit)
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
            }
            else
            {
                conventionalCommit.Subject = header;
            }

            for (var i =1;i<commitMessageLines.Count;i++) {
                foreach (var noteKeyword in noteKeywords)
                {
                    var line = commitMessageLines[i];
                    if (line.StartsWith($"{noteKeyword}:"))
                    {
                        conventionalCommit.Notes.Add(new ConventionalCommitNote
                        {
                            Title = noteKeyword,
                            Text = line.Substring($"{noteKeyword}:".Length).TrimStart()
                        });
                    }
                }
            }

            return conventionalCommit;
        }
    }
}
