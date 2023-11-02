﻿using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Versioning;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public class WorkingCopy
{
    private readonly DirectoryInfo _workingDirectory;
    private readonly DirectoryInfo _gitDirectory;

    private WorkingCopy(DirectoryInfo workingDirectory, DirectoryInfo gitDirectory)
    {
        _workingDirectory = workingDirectory;
        _gitDirectory = gitDirectory;
    }

    public SemanticVersion Inspect()
    {
        var workingDirectory = _workingDirectory.FullName;

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
        var workingDirectory = _workingDirectory.FullName;
        var gitDirectory = _gitDirectory.FullName;

        using var repo = new Repository(gitDirectory);

        var isDirty = repo.RetrieveStatus(new StatusOptions()).IsDirty;

        if (!options.SkipDirty && isDirty)
        {
            Exit($"Repository {workingDirectory} is dirty. Please commit your changes.", 1);
        }

        var projects = Projects.Discover(workingDirectory);

        if (projects.IsEmpty() && options.TagOnly is false)
        {
            Exit($"Could not find any projects files in {workingDirectory} that have a <Version> defined in their csproj file.", 1);
        }

        if (options.TagOnly is false && projects.HasInconsistentVersioning())
        {
            Exit($"Some projects in {workingDirectory} have an inconsistent <Version> defined in their csproj file. Please update all versions to be consistent or remove the <Version> elements from projects that should not be versioned", 1);
        }

        if (options.TagOnly is false)
        {
            Information($"Discovered {projects.GetProjectFiles().Count()} versionable projects");
            foreach (var project in projects.GetProjectFiles())
            {
                Information($"  * {project}");
            }    
        }

        if (options.TagOnly)
        {
            Information("Tagging only, no checking of projects or commits will occur");
        }

        var version = GetCurrentVersion(options, repo, projects);
        
        if (options.AggregatePrereleases)
        {
            version = repo
                .Tags
                .Select(tag =>
                {
                    SemanticVersion.TryParse(tag.FriendlyName[1..], out var v);
                    return v;
                })
                .Where(x => x != null && !x.IsPrerelease)
                .OrderByDescending(x => x.Major)
                .ThenByDescending(x => x.Minor)
                .ThenByDescending(x => x.Patch)
                .FirstOrDefault();
        }

        var isInitialRelease = false;
        List<Commit> commitsInVersion;
        if (options.UseProjVersionForBumpLogic)
        {
            var lastReleaseCommit = repo.Commits.FirstOrDefault(x => x.Message.StartsWith("chore(release):"));
            isInitialRelease = lastReleaseCommit is null;
            commitsInVersion = repo.GetCommitsSinceLastReleaseCommit();
        }
        else
        {
            var versionTag = repo.SelectVersionTag(version);
            isInitialRelease = versionTag == null;
            commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag);
        }

        var conventionalCommits = ConventionalCommitParser.Parse(commitsInVersion);

        var versionIncrement = new VersionIncrementStrategy(conventionalCommits);

        var allowInsignificantCommits = !(options.IgnoreInsignificantCommits || options.ExitInsignificantCommits);
        var nextVersion = isInitialRelease || version is null
            ? version ?? new SemanticVersion(1,0,0)
            : versionIncrement.NextVersion(version, options.Prerelease, allowInsignificantCommits);

        if (!isInitialRelease && nextVersion == version)
        {
            if (options.IgnoreInsignificantCommits || options.ExitInsignificantCommits)
            {
                var exitCode = options.ExitInsignificantCommits ? 1 : 0;
                Exit($"Version was not affected by commits since last release ({version})", exitCode);
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

        if (nextVersion < version)
        {
            Exit($"Semantic versioning conflict: the next version {nextVersion} would be lower than the current version {projects.Version}. This can be caused by using a wrong pre-release label or release as version", 1);
        }

        if (repo.VersionTagsExists(nextVersion) && !options.DryRun && !options.SkipCommit && !options.SkipTag)
        {
            Exit($"Version {nextVersion} already exists. Please use a different version.", 1);
        }

        var versionTime = DateTimeOffset.Now;

        if (!options.TagOnly)
        {
            Step($"bumping version from {projects.Version} to {nextVersion} in projects");   
        }
       
        // Commit changelog and version source
        if (options.TagOnly is false && !options.DryRun && (nextVersion != projects.Version))
        {
            projects.WriteVersion(nextVersion);

            foreach (var projectFile in projects.GetProjectFiles())
            {
                Commands.Stage(repo, projectFile);
            }
        }

        var changelog = ChangelogBuilder.CreateForPath(workingDirectory);
        var changelogLinkBuilder = LinkBuilderFactory.CreateFor(repo, options.Changelog.LinkTemplates);

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

        if (!options.DryRun && !options.SkipCommit &&  !options.TagOnly)
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

            if (!options.SkipTag)
            {
                repo.Tags.Add($"v{nextVersion}", versionCommit, author, $"{nextVersion}");
                Step($"tagged release as {nextVersion}");
            }

            Information("");
            Information("Run `git push --follow-tags origin main` to push all changes including tags");
        }
        else if (options.DryRun is false && options.TagOnly)
        {
            var commitToTag = repo.Commits.QueryBy(new CommitFilter
            {
                SortBy = CommitSortStrategies.Time
            }).First();
            
            repo.Tags.Add($"v{nextVersion}", commitToTag , repo.Config.BuildSignature(versionTime), $"{nextVersion}");
            Step($"tagged release as {nextVersion} against commit with sha {commitToTag.Sha}");
        }
        else if (options.SkipCommit)
        {
            Information("");
            Information($"Commit and tagging of release was skipped. Tag this release as `v{nextVersion}` to make versionize detect the release");
        }

        return nextVersion;
    }

    private static SemanticVersion GetCurrentVersion(VersionizeOptions options, Repository repo, Projects projects)
    {
        SemanticVersion version;
        if (options.TagOnly)
        {
            version = repo.Tags
                .Select(tag =>
                {
                    SemanticVersion.TryParse(tag.FriendlyName[1..], out var v);
                    return v;
                })
                .Where(x => x != null)
                .OrderByDescending(x => x.Major)
                .ThenByDescending(x => x.Minor)
                .ThenByDescending(x => x.Patch)
                .FirstOrDefault();
        }
        else
        {
            version = projects.Version;
        }

        return version;
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
                return new WorkingCopy(new DirectoryInfo(workingDirectory), workingCopyCandidate);
            }

            workingCopyCandidate = workingCopyCandidate.Parent;
        }
        while (workingCopyCandidate.Parent != null);

        Exit($"Directory {workingDirectory} or any parent directory do not contain a git working copy", 3);

        return null;
    }
}
