using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Versionize
{
    public class Changelog
    {
        private readonly FileInfo _file;

        private const string Preamble = @"# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/saintedlama/versionize) for commit guidelines.
";

        private Changelog(FileInfo file)
        {
            _file = file;
        }

        public string FilePath { get => _file.FullName; }

        public void Write(Version version, DateTimeOffset versionTime, IEnumerable<ConventionalCommit> commits,
            bool includeAllCommitsInChangelog = false)
        {
            // TODO: Implement a gitish version reference builder - bitbucket / github
            var markdown = $"<a name=\"{version}\"></a>";
            markdown += "\n";
            markdown += $"## {version} ({versionTime.Year}-{versionTime.Month}-{versionTime.Day})";
            markdown += "\n";
            markdown += "\n";

            var bugFixes = BuildBlock("Bug Fixes", commits.Where(commit => commit.IsFix));

            if (!String.IsNullOrWhiteSpace(bugFixes))
            {
                markdown += bugFixes;
                markdown += "\n";
            }

            var features = BuildBlock("Features", commits.Where(commit => commit.IsFeature));

            if (!String.IsNullOrWhiteSpace(features))
            {
                markdown += features;
                markdown += "\n";
            }

            var breaking = BuildBlock("Breaking Changes", commits.Where(commit => commit.IsBreakingChange));

            if (!String.IsNullOrWhiteSpace(breaking))
            {
                markdown += breaking;
                markdown += "\n";
            }

            if (includeAllCommitsInChangelog)
            {
                var other = BuildBlock("Other", commits.Where(commit => !commit.IsFix && !commit.IsFeature && !commit.IsBreakingChange));

                if (!string.IsNullOrWhiteSpace(other))
                {
                    markdown += other;
                    markdown += "\n";
                }
            }

            if (_file.Exists)
            {
                var contents = File.ReadAllText(_file.FullName);

                var firstReleaseHeadlineIdx = contents.IndexOf("##");

                if (firstReleaseHeadlineIdx >= 0)
                {
                    contents = contents.Substring(firstReleaseHeadlineIdx);
                }

                markdown += contents;
            }

            File.WriteAllText(_file.FullName, Preamble + "\n" + markdown);
        }

        public static string BuildBlock(string header, IEnumerable<ConventionalCommit> commits)
        {
            if (!commits.Any())
            {
                return null;
            }

            var block = $"### {header}";
            block += "\n";
            block += "\n";

            return commits.Aggregate(block, (current, commit) => current + $"* {commit.Subject}\n");
        }

        public static Changelog Discover(string directory)
        {
            var changelogFile = new FileInfo(Path.Combine(directory, "CHANGELOG.md"));

            return new Changelog(changelogFile);
        }
    }
}
