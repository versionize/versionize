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

        public void Write(Version version, DateTimeOffset versionTime, IEnumerable<ConventionalCommit> commits)
        {
            // TODO: Implement a gitish version reference builder - bitbucket / github
            var markdown = $"<a name=\"{version}\"></a>";
            markdown += "\n";
            markdown += $"## [{version}] ({versionTime.Year}-{versionTime.Month}-{versionTime.Day})";
            markdown += "\n";

            var bugFixes = BuildBlock("Bug Fixes", commits.Where(commit => "fix".Equals(commit.Type)));

            if (!String.IsNullOrWhiteSpace(bugFixes))
            {
                markdown += bugFixes;
                markdown += "\n";
            }

            var features = BuildBlock("Features", commits.Where(commit => "feat".Equals(commit.Type)));

            if (!String.IsNullOrWhiteSpace(features))
            {
                markdown += features;
                markdown += "\n";
            }

            var breaking = BuildBlock("Breaking Changes", commits.Where(commit => commit.Notes.Any(note => "BREAKING CHANGE".Equals(note.Title))));

            if (!String.IsNullOrWhiteSpace(breaking))
            {
                markdown += breaking;
                markdown += "\n";
            }

            if (_file.Exists)
            {
                var contents = File.ReadAllText(_file.FullName);

                var preambleIdx = contents.IndexOf(Preamble);

                if (preambleIdx >= 0)
                {
                    contents = contents.Substring(preambleIdx);
                }

                markdown += contents;
            }

            File.WriteAllText(_file.FullName, Preamble + "\n" + markdown);
        }

        public string BuildBlock(string header, IEnumerable<ConventionalCommit> commits)
        {
            if (!commits.Any())
            {
                return null;
            }

            var block = $"### {header}";
            block += "\n";

            foreach (var commit in commits)
            {
                block += $"* {commit.Subject}\n";
            }

            return block;
        }

        public static Changelog Discover(string directory)
        {
            var changelogFile = new FileInfo(Path.Combine(directory, "CHANGELOG.md"));

            return new Changelog(changelogFile);
        }
    }
}
