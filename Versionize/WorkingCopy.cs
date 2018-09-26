using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using static Versionize.ConsoleUI;

namespace Versionize
{
    public class WorkingCopy
    {
        private readonly DirectoryInfo _directory;

        private WorkingCopy(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public void Versionize()
        {
            var workingDirectory = _directory.FullName;

            using (var repo = new Repository(workingDirectory))
            {
                var isDirty = repo.RetrieveStatus(new StatusOptions()).IsDirty;

                if (isDirty)
                {
                    Exit($"Repository {workingDirectory} is dirty. Please commit your changes.", -1);
                }

                var projects = Projects.Discover(workingDirectory);

                var versionTag = repo.SelectVersionTag(projects.Version);
                var commitsInVersion = repo.GteCommitsSinceLastVersion(versionTag);

                var commitParser = new ConventionalCommitParser();
                var conventionalCommits = commitParser.Parse(commitsInVersion);

                var versionIncrement = VersionIncrement.CreateFrom(conventionalCommits);

                var nextVersion = versionTag == null ? projects.Version : versionIncrement.NextVersion(projects.Version);

                var versionTime = DateTimeOffset.Now;

                // Commit changelog and version source
                if (nextVersion != projects.Version)
                {
                    projects.WriteVersion(nextVersion);

                    foreach (var projectFile in projects.GetProjectFiles())
                    {
                        Commands.Stage(repo, projectFile);
                    }
                }

                //var changelog = ChangeLog.Locate(workingDirectory);
                //changelog.WriteChangeLogIncrement(versionToRelease, versionTime, commitsSinceLastVersion);
                //Commands.Stage(repo, changelog.Location);

                var author = repo.Config.BuildSignature(versionTime);
                Signature committer = author;

                // TODO: Check if tag exists before commit
                var releaseCommitMessage = $"chore(release): {nextVersion}";
                Commit versionCommit = repo.Commit(releaseCommitMessage, author, committer);
                Tag newTag = repo.Tags.Add($"v{nextVersion}", versionCommit, author, $"{nextVersion}");

                Console.WriteLine($"{projects.Version} -> {nextVersion}");
            }
        }

        public static WorkingCopy Discover(string workingDirectory)
        {
            var workingCopyCandidate = new DirectoryInfo(workingDirectory);

            if (!workingCopyCandidate.Exists)
            {
                throw new InvalidOperationException($"Directory {workingDirectory} does not exist");
            }

            do
            {
                var isWorkingCopy = workingCopyCandidate.GetDirectories(".git").Any();

                if (isWorkingCopy)
                {
                    return new WorkingCopy(workingCopyCandidate);
                }

                workingCopyCandidate = workingCopyCandidate.Parent;
            }
            while (workingCopyCandidate.Parent != null);

            throw new InvalidOperationException($"Directory {workingDirectory} or any parent directory do not contain a git working copy");
        }
    }
}