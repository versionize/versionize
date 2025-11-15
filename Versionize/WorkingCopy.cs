using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.Git;
using Versionize.Lifecycle;
using static Versionize.CommandLine.CommandLineUI;
using Versionize.CommandLine;

namespace Versionize;

public sealed class WorkingCopy
{
    private readonly DirectoryInfo _workingDirectory;
    private readonly DirectoryInfo _gitDirectory;

    private WorkingCopy(DirectoryInfo workingDirectory, DirectoryInfo gitDirectory)
    {
        _workingDirectory = workingDirectory;
        _gitDirectory = gitDirectory;
    }

    public SemanticVersion Inspect(VersionizeOptions options)
    {
        // TODO: Consider implementing "--tag-only" variation
        options = options with { WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path) };

        Verbosity = CommandLine.LogLevel.Error;
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        Verbosity = CommandLine.LogLevel.All;
        Information(bumpFile.Version.ToNormalizedString());
        return bumpFile.Version;
    }

    public void GenerateChangelog(VersionizeOptions options, string? versionStr, string? preamble)
    {
        options = options with { WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path) };

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
        options = options with { WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path) };

        using Repository repo = ValidateRepoState(options, options.WorkingDirectory);
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        var version = repo.GetCurrentVersion(options, bumpFile);
        var (isInitialRelease, conventionalCommits) = ConventionalCommitProvider.GetCommits(repo, options, version);
        var newVersion = VersionCalculator.Bump(options, version, isInitialRelease, conventionalCommits);
        BumpFileUpdater.Update(options, newVersion, bumpFile);
        var changelog = ChangelogUpdater.Update(repo, options, newVersion, version, conventionalCommits);
        ChangeCommitter.CreateCommit(repo, options, newVersion, bumpFile, changelog);
        ReleaseTagger.CreateTag(repo, options, newVersion);
    }

    private Repository ValidateRepoState(VersionizeOptions options, string workingDirectory)
    {
        var gitDirectory = _gitDirectory.FullName;
        var repo = new Repository(gitDirectory);

        // Only check Git configuration if we will perform Git operations (commit or tag)
        if (options.IsCommitConfigurationRequired() && !repo.IsConfiguredForCommits())
        {
            throw new VersionizeException(ErrorMessages.GitConfigMissing(), 1);
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
            throw new VersionizeException(ErrorMessages.RepositoryDirty(workingDirectory, dirtyFilesString), 1);
        }

        return repo;
    }

    public static WorkingCopy? Discover(string workingDirectoryPath)
    {
        var workingDirectory = new DirectoryInfo(workingDirectoryPath);

        if (!workingDirectory.Exists)
        {
            throw new VersionizeException(ErrorMessages.RepositoryDoesNotExist(workingDirectory.FullName), 2);
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
        } while (currentDirectory is { Parent: not null });

        throw new VersionizeException(ErrorMessages.RepositoryNotGit(workingDirectory.FullName), 3);
    }
}
