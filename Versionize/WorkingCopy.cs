using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Versionize.Changelog;
using static Versionize.CommandLine.CommandLineUI;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize
{
    public class WorkingCopy
    {
        private readonly DirectoryInfo _directory;

        private WorkingCopy(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public Version Versionize(bool dryrun = false,
            bool skipDirtyCheck = false,
            bool skipCommit = false,
            string releaseVersion = null,
            bool ignoreInsignificant = false,
            bool includeAllCommitsInChangelog = false,
            string releaseCommitMessageSuffix = null)
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

                if (projects.IsEmpty())
                {
                    Exit($"Could not find any projects files in {workingDirectory} that have a <Version> defined in their csproj file.", 1);
                }

                if (projects.HasInconsistentVersioning())
                {
                    Exit($"Some projects in {workingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned", 1);
                }

                Information($"Discovered {projects.GetProjectFiles().Count()} versionable projects");
                foreach (var project in projects.GetProjectFiles())
                {
                    Information($"  * {project}");
                }

                var versionTag = repo.SelectVersionTag(projects.Version);
                var commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag);

                var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion);

                var versionIncrement = VersionIncrementStrategy.CreateFrom(conventionalCommits);

                var nextVersion = versionTag == null ? projects.Version : versionIncrement.NextVersion(projects.Version, ignoreInsignificant);

                if (ignoreInsignificant && nextVersion == projects.Version)
                {
                    Exit($"Version was not affected by commits since last release ({projects.Version}), since you specified to ignore insignificant changes, no action will be performed.", 0);
                }

                if (!string.IsNullOrWhiteSpace(releaseVersion))
                {
                    try
                    {
                        nextVersion = Version.Parse(releaseVersion);
                    }
                    catch (Exception)
                    {
                        Exit($"Could not parse the specified release version {releaseVersion} as valid version", 1);
                    }
                }

                var versionTime = DateTimeOffset.Now;

                // Commit changelog and version source
                if (!dryrun && (nextVersion != projects.Version))
                {
                    projects.WriteVersion(nextVersion);

                    foreach (var projectFile in projects.GetProjectFiles())
                    {
                        Commands.Stage(repo, projectFile);
                    }
                }

                Step($"bumping version from {projects.Version} to {nextVersion} in projects");

                var changelog = ChangelogBuilder.CreateForPath(workingDirectory);

                if (!dryrun)
                {
                    var changelogLinkBuilder = ChangelogLinkBuilderFactory.CreateFor(repo);
                    changelog.Write(nextVersion, versionTime, changelogLinkBuilder, conventionalCommits, includeAllCommitsInChangelog);
                }

                Step("updated CHANGELOG.md");

                if (!dryrun && !skipCommit)
                {
                    Commands.Stage(repo, changelog.FilePath);

                    foreach (var projectFile in projects.GetProjectFiles())
                    {
                        Commands.Stage(repo, projectFile);
                    }
                }

                if (!dryrun && !skipCommit)
                {
                    var author = repo.Config.BuildSignature(versionTime);
                    var committer = author;

                    // TODO: Check if tag exists before commit
                    var releaseCommitMessage = $"chore(release): {nextVersion} {releaseCommitMessageSuffix}".TrimEnd();
                    var versionCommit = repo.Commit(releaseCommitMessage, author, committer);
                    Step("committed changes in projects and CHANGELOG.md");

                    repo.Tags.Add($"v{nextVersion}", versionCommit, author, $"{nextVersion}");
                    Step($"tagged release as {nextVersion}");

                    Information("");
                    Information("i Run `git push --follow-tags origin master` to push all changes including tags");
                }
                else if (skipCommit)
                {
                    Information("");
                    Information($"i Commit and tagging of release was skipped. Tag this release as `v{nextVersion}` to make versionize detect the release");
                }

                return nextVersion;
            }
        }

        public static WorkingCopy Discover(string workingDirectory)
        {
            var workingCopyCandidate = new DirectoryInfo(workingDirectory);

            if (!workingCopyCandidate.Exists)
            {
                Exit($"Directory {workingDirectory} does not exist", 2);
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

            Exit($"Directory {workingDirectory} or any parent directory do not contain a git working copy", 3);

            return null;
        }
    }
}
