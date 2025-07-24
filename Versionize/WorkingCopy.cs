using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
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
        Verbosity = CommandLine.LogLevel.Error;
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        Verbosity = CommandLine.LogLevel.All;
        Information(bumpFile.Version.ToNormalizedString());
        return bumpFile.Version;
    }

    public void GenerateChanglog(VersionizeOptions options, string? versionStr, string? preamble)
    {
        options.WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path);

        Verbosity = CommandLine.LogLevel.Error;
        using Repository repo = ValidateRepoState(options, options.WorkingDirectory);
        var (FromRef, ToRef) = repo.GetCommitRange(versionStr, options);
        var conventionalCommits = ConventionalCommitProvider.GetCommits(repo, options, FromRef, ToRef);
        var linkBuilder = LinkBuilderFactory.CreateFor(repo, options.Project.Changelog.LinkTemplates);
        string markdown = ChangelogBuilder.GenerateCommitList(
            linkBuilder,
            conventionalCommits,
            options.Project.Changelog);
        Verbosity = CommandLine.LogLevel.All;
        var changelog = preamble + markdown.TrimEnd();

        Information(changelog);
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
