using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Versionize
{
    public class Changelog
    {
        private const string Preamble = @"# Change Log\n\nAll notable changes to this project will be documented in this file. See [versionize](https://github.com/saintedlama/versionize) for commit guidelines.\n";

        private Changelog(string file)
        {
            FilePath = file;
        }

        public string FilePath { get; }

        public void Write(Version version, DateTimeOffset versionTime, IChangelogLinkBuilder linkBuilder, IEnumerable<ConventionalCommit> commits,
            bool includeAllCommitsInChangelog = false)
        {
            var versionTagLink = string.IsNullOrWhiteSpace(linkBuilder.BuildVersionTagLink(version)) ? version.ToString() : $"[{version}]({linkBuilder.BuildVersionTagLink(version)})";

            var markdown = $"<a name=\"{version}\"></a>";
            markdown += "\n";
            markdown += $"## {versionTagLink} ({versionTime.Year}-{versionTime.Month}-{versionTime.Day})";
            markdown += "\n";
            markdown += "\n";

            var bugFixes = BuildBlock("Bug Fixes", linkBuilder, commits.Where(commit => commit.IsFix));

            if (!string.IsNullOrWhiteSpace(bugFixes))
            {
                markdown += bugFixes;
                markdown += "\n";
            }

            var features = BuildBlock("Features", linkBuilder, commits.Where(commit => commit.IsFeature));

            if (!string.IsNullOrWhiteSpace(features))
            {
                markdown += features;
                markdown += "\n";
            }

            var breaking = BuildBlock("Breaking Changes", linkBuilder, commits.Where(commit => commit.IsBreakingChange));

            if (!string.IsNullOrWhiteSpace(breaking))
            {
                markdown += breaking;
                markdown += "\n";
            }

            if (includeAllCommitsInChangelog)
            {
                var other = BuildBlock("Other", linkBuilder, commits.Where(commit => !commit.IsFix && !commit.IsFeature && !commit.IsBreakingChange));

                if (!string.IsNullOrWhiteSpace(other))
                {
                    markdown += other;
                    markdown += "\n";
                }
            }

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
                File.WriteAllText(FilePath, Preamble + "\n" + markdown);
            }
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

        public static Changelog Discover(string directory)
        {
            var changelogFile = Path.Combine(directory, "CHANGELOG.md");

            return new Changelog(changelogFile);
        }
    }
}
