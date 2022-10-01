using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Versioning;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public class WorkingCopy
{
    private readonly DirectoryInfo _directory;

    private WorkingCopy(DirectoryInfo directory)
    {
        _directory = directory;
    }

    public SemanticVersion Inspect()
    {
        var workingDirectory = _directory.FullName;

        var projects = Projects.Discover(workingDirectory);

        if (projects.IsEmpty())
        {
            Exit($"Could not find any projects files in {workingDirectory} that have a <Version> defined in their csproj file.", 1);
        }

        if (projects.HasInconsistentVersioning())
        {
            Exit($"Some projects in {workingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned", 1);
        }

        Information(projects.Version.ToNormalizedString());

        return projects.Version;
    }

    public SemanticVersion Versionize(VersionizeOptions options)
    {
        var workingDirectory = _directory.FullName;

        using var repo = new Repository(workingDirectory);

        var isDirty = repo.RetrieveStatus(new StatusOptions()).IsDirty;

        if (!options.SkipDirty && isDirty)
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

        var version = projects.Version;
        if (options.AggregatePrereleases)
        {
            version = repo
                .Tags
                .Select(tag =>
                {
                    SemanticVersion.TryParse(tag.FriendlyName[1..], out var version);
                    return version;
                })
                .Where(x => x != null && !x.IsPrerelease)
                .OrderByDescending(x => x.Major)
                .ThenByDescending(x => x.Minor)
                .ThenByDescending(x => x.Patch)
                .FirstOrDefault();
            Console.WriteLine(version);
        }

        var versionTag = repo.SelectVersionTag(version);
        var isInitialRelease = versionTag == null;
        var commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag);

        var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion);

        var versionIncrement = new VersionIncrementStrategy(conventionalCommits);

        var nextVersion = isInitialRelease ? projects.Version : versionIncrement.NextVersion(projects.Version, options.Prerelease);

        // For non initial releases: for insignificant commits such as chore increment the patch version if IgnoreInsignificantCommits is not set
        if (!isInitialRelease && nextVersion == projects.Version)
        {
            if (options.IgnoreInsignificantCommits || options.ExitInsignificantCommits)
            {
                var exitCode = options.ExitInsignificantCommits ? 1 : 0;
                Exit($"Version was not affected by commits since last release ({projects.Version})", exitCode);
            }
            else
            {
                nextVersion = nextVersion.IncrementPatchVersion();
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ReleaseAs))
        {
            try
            {
                nextVersion = SemanticVersion.Parse(options.ReleaseAs);
            }
            catch (Exception)
            {
                Exit($"Could not parse the specified release version {options.ReleaseAs} as valid version", 1);
            }
        }

        if (nextVersion < projects.Version)
        {
            Exit($"Semantic versioning conflict: the next version {nextVersion} would be lower than the current version {projects.Version}. This can be caused by using a wrong pre-release label or release as version", 1);
        }

        if (!options.DryRun && !options.SkipCommit && repo.VersionTagsExists(nextVersion))
        {
            Exit($"Version {nextVersion} already exists. Please use a different version.", 1);
        }

        var versionTime = DateTimeOffset.Now;


        Step($"bumping version from {projects.Version} to {nextVersion} in projects");
       
        // Commit changelog and version source
        if (!options.DryRun && (nextVersion != projects.Version))
        {
            projects.WriteVersion(nextVersion);

            foreach (var projectFile in projects.GetProjectFiles())
            {
                Commands.Stage(repo, projectFile);
            }
        }

        var changelog = ChangelogBuilder.CreateForPath(workingDirectory);
        var changelogLinkBuilder = LinkBuilderFactory.CreateFor(repo);

        if (options.DryRun)
        {
            string markdown = ChangelogBuilder.GenerateMarkdown(nextVersion, versionTime, changelogLinkBuilder, conventionalCommits, options.Changelog);
            DryRun(markdown.TrimEnd('\n'));
        }
        else
        {
            changelog.Write(nextVersion, versionTime, changelogLinkBuilder, conventionalCommits, options.Changelog);
        }

        Step("updated CHANGELOG.md");

        if (!options.DryRun && !options.SkipCommit)
        {
            if (!repo.IsConfiguredForCommits())
            {
                Exit(@"Warning: Git configuration is missing. Please configure git before running versionize:
git config --global user.name ""John Doe""
$ git config --global user.email johndoe@example.com", 1);
            }

            Commands.Stage(repo, changelog.FilePath);

            foreach (var projectFile in projects.GetProjectFiles())
            {
                Commands.Stage(repo, projectFile);
            }

            var author = repo.Config.BuildSignature(versionTime);
            var committer = author;

            var releaseCommitMessage = $"chore(release): {nextVersion} {options.CommitSuffix}".TrimEnd();
            var versionCommit = repo.Commit(releaseCommitMessage, author, committer);
            Step("committed changes in projects and CHANGELOG.md");

            repo.Tags.Add($"v{nextVersion}", versionCommit, author, $"{nextVersion}");
            Step($"tagged release as {nextVersion}");

            Information("");
            Information("i Run `git push --follow-tags origin master` to push all changes including tags");
        }
        else if (options.SkipCommit)
        {
            Information("");
            Information($"i Commit and tagging of release was skipped. Tag this release as `v{nextVersion}` to make versionize detect the release");
        }

        return nextVersion;
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
