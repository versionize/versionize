using LibGit2Sharp;
using NuGet.Versioning;
using System.Text.RegularExpressions;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Git;
using Versionize.Lifecycle;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public class WorkingCopy
{
    private readonly DirectoryInfo _workingDirectory;
    private readonly DirectoryInfo _gitDirectory;

    private WorkingCopy(
        DirectoryInfo workingDirectory,
        DirectoryInfo gitDirectory)
    {
        _workingDirectory = workingDirectory;
        _gitDirectory = gitDirectory;
    }

    public SemanticVersion Inspect(VersionizeOptions options)
    {
        // TODO: Implement "--tag-only" variation
        options.WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path);
        CommandLineUI.Verbosity = CommandLine.LogLevel.Error;
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        CommandLineUI.Verbosity = CommandLine.LogLevel.All;
        Information(bumpFile.Version.ToNormalizedString());
        return bumpFile.Version;
    }

    public void GenerateChanglog(VersionizeOptions options, string? rangeStr)
    {
        options.WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path);

        using Repository repo = ValidateRepoState(options, options.WorkingDirectory);

        // parse range
        // if null, <from> defaults to latest version tag, and <to> defaults to HEAD
        // format is <from>..<to> (should include one or both <from> and <to>)
        // if <from> is not specified, it defaults to first commit
        // if <to> is not specified, it defaults to HEAD
        // ..sha, sha.., sha..sha
        // version, version.., version..version
        // 1m.., 7d..
        // where <from> and <to> can be a version, a sha, or <n><unit> where <unit> is one of: d: day, m: month, v: version
        // when used as <from>, <n><unit> means minus n units from HEAD
        // when used as <to>, <n><unit> means plus n units from <from>
        string? from = null;
        string? to = null;
        if (rangeStr != null)
        {
            var parts = rangeStr.Split("..");
            if (parts.Length > 2)
            {
                Exit("Invalid range format. Expected <from>..<to>", 1);
            }
            if (parts.Length == 1)
            {
                // if parts[0] is version, from = GetPreviousTag(parts[0]), to = GetTag(parts[0])
                // if parts[0] is sha, from = sha, to = HEAD
                // if parts[0] is <n><unit>, from = GetOldestCommitWithinLastXUnits(n), to = HEAD
                (from, to) = GetShaRange(repo, parts[0], options);
            }

            from = parts[0];
            to = parts[1];
        }

        //var fromRef = GetFromSha(repo, from, options.Project);
        //var toRef = GetToSha(repo, to, options.Project);

        ////var version = repo.GetCurrentVersion(options);
        //var version = SemanticVersion.Parse("1.23.0");

        //var versionToUseForCommitDiff = version;
        //if (options.AggregatePrereleases)
        //{
        //    versionToUseForCommitDiff = repo
        //        .Tags
        //        .Select(options.Project.ExtractTagVersion)
        //        .Where(x => x != null && !x.IsPrerelease)
        //        .OrderByDescending(x => x!.Major)
        //        .ThenByDescending(x => x!.Minor)
        //        .ThenByDescending(x => x!.Patch)
        //        .FirstOrDefault();
        //}

        //var fromRef = repo.SelectVersionTag(versionToUseForCommitDiff, options.Project)?.Target;
        //var toRef = repo.Head.Tip;

        //var conventionalCommits = ConventionalCommitProvider.GetCommits(repo, options, fromRef, toRef);
        //var linkBuilder = LinkBuilderFactory.CreateFor(repo, options.Project.Changelog.LinkTemplates);
        //string markdown = ChangelogBuilder.GenerateCommitList(
        //    linkBuilder,
        //    conventionalCommits,
        //    options.Project.Changelog);

        //Information(markdown);
    }

    //export default class GitHubMarkdown implements IMarkdown
    //{
    //  heading(text: string, level: number): string {
    //    return `${'#'.repeat(level)} ${text}\n\n`
    //  }

    //  bold(text: string): string {
    //    return `**${text}**`
    //  }

    //  link(display: string, link: string): string {
    //    return `[${display}](${ link})`
    //  }

    //  ul(list: string[]): string {
    //    return `* ${list.join('\n* ')}\n\n`
    //  }
    //}

    private static (string From, string To) GetShaRange(Repository repo, string refStr, VersionizeOptions options)
    {
        if (SemanticVersion.TryParse(refStr, out var version))
        {
            var toTag = repo.SelectVersionTag(version, options.Project);
            if (toTag is null)
            {
                throw new ArgumentException($"Version {version} not found");
            }
            var fromVersion = repo.GetPreviousVersion(version, options);
            GitObject? fromRef = repo.SelectVersionTag(fromVersion, options.Project)?.Target ??
                repo.GetOldestCommitSinceDate(DateTimeOffset.MinValue);
            if (fromRef is null)
            {
                throw new ArgumentException("No previous version found");
            }
            return (fromRef.Sha, toTag.Target.Sha);
        }

        var unitRegex = new Regex(@"^(\d+)([dmv])$");
        var match = unitRegex.Match(refStr);
        if (match.Success)
        {
            var count = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;
            GitObject? fromRef = unit switch
            {
                "d" => repo.GetOldestCommitWithinLastXDays(count),
                "m" => repo.GetOldestCommitWithinLastXMonths(count),
                "v" => repo.GetNthMostRecentVersionTag(count)?.Target,
                _ => throw new ArgumentException("Invalid unit")
            };
            if (fromRef is null)
            {
                throw new ArgumentException("No commits found");
            }
            return (fromRef.Sha, repo.Head.Tip.Sha);
        }

        if (repo.Lookup<Commit>(refStr) is Commit commit)
        {
            return (commit.Sha, repo.Head.Tip.Sha);
        }

        throw new ArgumentException("Invalid <from> format");
    }

    private static (string From, string To) GetShaRange(Repository repo, string from, string to, ProjectOptions project)
    {
        string fromRef = "";
        if (string.IsNullOrEmpty(from))
        {
            // get very first commit
            fromRef = repo.GetOldestCommitSinceDate(DateTimeOffset.MinValue)?.Sha ?? throw new ArgumentException("No commits found");
        }
        string toRef = "";
        if (string.IsNullOrEmpty(to))
        {
            toRef = repo.Head.Tip.Sha;
        }

        if (SemanticVersion.TryParse(from, out var version))
        {
            var tag = repo.SelectVersionTag(version, project);
            fromRef = tag?.Target.Sha ?? throw new ArgumentException($"Version {version} not found");
        }

        var unitRegex = new Regex(@"^(\d+)([dmv])$");
        var match = unitRegex.Match(from);
        if (match.Success)
        {
            var count = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;
            GitObject? fromRef = unit switch
            {
                "d" => repo.GetOldestCommitWithinLastXDays(count),
                "m" => repo.GetOldestCommitWithinLastXMonths(count),
                "v" => repo.GetNthMostRecentVersionTag(count)?.Target,
                _ => throw new ArgumentException("Invalid unit")
            };
            if (fromRef is null)
            {
                throw new ArgumentException("No commits found");
            }
            return (fromRef.Sha, repo.Head.Tip.Sha);
        }

        if (repo.Lookup<Commit>(from) is Commit commit)
        {
            fromRef = commit.Sha;
        }

        //throw new ArgumentException("Invalid <from> format");
        return null;
    }

    public void Versionize(VersionizeOptions options)
    {
        options.WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path);

        using Repository repo = ValidateRepoState(options, options.WorkingDirectory);
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        var version = repo.GetCurrentVersion(options, bumpFile);
        var (isInitialRelease, conventionalCommits) = ConventionalCommitProvider.GetCommits(repo, options, version);
        var newVersion = VersionCalculator.Bump(options, version, isInitialRelease, conventionalCommits);
        BumpFileUpdater.Update(options, newVersion, bumpFile);
        var changelog = ChangelogUpdater.Update(repo, options, newVersion, conventionalCommits);
        ChangeCommitter.CreateCommit(repo, options, newVersion, bumpFile, changelog);
        ReleaseTagger.CreateTag(repo, options, newVersion);
    }

    private Repository ValidateRepoState(VersionizeOptions options, string workingDirectory)
    {
        var gitDirectory = _gitDirectory.FullName;
        var repo = new Repository(gitDirectory);

        if (!repo.IsConfiguredForCommits())
        {
            Exit(@"Warning: Git configuration is missing. Please configure git before running versionize:
git config --global user.name ""John Doe""
$ git config --global user.email johndoe@example.com", 1);
        }

        if (options.SkipCommit)
        {
            return repo;
        }

        var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = false });
        if (status.IsDirty && !options.SkipDirty)
        {
            var dirtyFiles = status.Where(x => x.State != FileStatus.Ignored).Select(x => $"{x.State}: {x.FilePath}");
            var dirtyFilesString = string.Join(Environment.NewLine, dirtyFiles);
            Exit($"Repository {workingDirectory} is dirty. Please commit your changes:\n{dirtyFilesString}", 1);
        }

        return repo;
    }

    public static WorkingCopy? Discover(string workingDirectoryPath)
    {
        var workingDirectory = new DirectoryInfo(workingDirectoryPath);

        if (!workingDirectory.Exists)
        {
            Exit($"Directory {workingDirectory} does not exist", 2);
        }

        var currentDirectory = workingDirectory;
        do
        {
            var foundGitDirectory = currentDirectory.GetDirectories(".git").Length != 0;
            if (foundGitDirectory)
            {
                return new WorkingCopy(workingDirectory, gitDirectory: currentDirectory);
            }

            currentDirectory = currentDirectory.Parent;
        }
        while (currentDirectory is not null && currentDirectory.Parent != null);

        Exit($"Directory {workingDirectory} or any parent directory do not contain a git working copy", 3);

        return null;
    }
}
