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

        public void Versionize(bool dryrun = false, bool skipDirtyCheck = false)
        {
            var workingDirectory = _directory.FullName;

            using (var repo = new Repository(workingDirectory))
            {
                var isDirty = repo.RetrieveStatus(new StatusOptions()).IsDirty;

                if (!skipDirtyCheck && isDirty)
                {
                    Exit($"Repository {workingDirectory} is dirty. Please commit your changes.", 1);
                }

                var projects = Projects.Discover(workingDirectory);

                ConsoleUI.Information($"Discovered {projects.GetProjectFiles().Count()} versionable projects");
                foreach (var project in projects.GetProjectFiles())
                {
                    ConsoleUI.Information($"  * {project}");
                }

                var versionTag = repo.SelectVersionTag(projects.Version);
                var commitsInVersion = repo.GteCommitsSinceLastVersion(versionTag);

                var commitParser = new ConventionalCommitParser();
                var conventionalCommits = commitParser.Parse(commitsInVersion);

                var versionIncrement = VersionIncrementStrategy.CreateFrom(conventionalCommits);

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

                ConsoleUI.Step($"bumping version from {projects.Version} to {nextVersion} in projects");

                var changelog = Changelog.Discover(workingDirectory);

                if (!dryrun)
                {
                    changelog.Write(nextVersion, versionTime, conventionalCommits);
                }

                ConsoleUI.Step($"updated CHANGELOG.md");

                if (!dryrun)
                {
                    Commands.Stage(repo, changelog.FilePath);

                    foreach (var projectFile in projects.GetProjectFiles())
                    {
                        Commands.Stage(repo, projectFile);
                    }
                }

                if (!dryrun)
                {

                    var author = repo.Config.BuildSignature(versionTime);
                    var committer = author;

                    // TODO: Check if tag exists before commit
                    var releaseCommitMessage = $"chore(release): {nextVersion}";
                    Commit versionCommit = repo.Commit(releaseCommitMessage, author, committer);

                    Tag newTag = repo.Tags.Add($"v{nextVersion}", versionCommit, author, $"{nextVersion}");
                }

                ConsoleUI.Step($"committed changes in projects and CHANGELOG.md");
                ConsoleUI.Step($"tagged release as {nextVersion}");

                ConsoleUI.Information($"i Run `git push --follow-tags origin master` to push all changes including tags");
            }
        }

        public static WorkingCopy Discover(string workingDirectory)
        {
            var workingCopyCandidate = new DirectoryInfo(workingDirectory);

            if (!workingCopyCandidate.Exists)
            {
                ConsoleUI.Exit($"Directory {workingDirectory} does not exist", 2);
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

            ConsoleUI.Exit($"Directory {workingDirectory} or any parent directory do not contain a git working copy", 3);

            return null;
        }
    }
}