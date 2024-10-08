using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.Git;
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

    public SemanticVersion Inspect()
    {
        return Inspect(ProjectOptions.DefaultOneProjectPerRepo);
    }

    public SemanticVersion Inspect(ProjectOptions projectOptions)
    {
        var workingDirectory = Path.Combine(_workingDirectory.FullName, projectOptions.Path);

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

    public void Versionize(VersionizeOptions options)
    {
        options.WorkingDirectory = Path.Combine(_workingDirectory.FullName, options.Project.Path);

        using Repository repo = ValidateRepoState(options, options.WorkingDirectory);
        var bumpFile = BumpFileProvider.GetBumpFile(options);
        var version = repo.GetCurrentVersion(options, bumpFile);
        var (isInitialRelease, conventionalCommits) = ConventionalCommitProvider.GetCommits(repo, options, version);
        var newVersion = VersionCalculator.Bump(options, version, isInitialRelease, conventionalCommits);
        bumpFile.Update(options, newVersion);
        var changelog = ChangelogUpdater.Update(repo, options, newVersion, conventionalCommits);
        ChangeCommitter.CreateCommit(repo, options, newVersion, bumpFile, changelog);
        ReleaseTagger.CreateTag(repo, options, newVersion);
    }

    private Repository ValidateRepoState(VersionizeOptions options, string workingDirectory)
    {
        var gitDirectory = _gitDirectory.FullName;
        var repo = new Repository(gitDirectory);

        if (options.SkipCommit || options.TagOnly)
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

    public static WorkingCopy Discover(string workingDirectoryPath)
    {
        var workingDirectory = new DirectoryInfo(workingDirectoryPath);

        if (!workingDirectory.Exists)
        {
            Exit($"Directory {workingDirectory} does not exist", 2);
        }

        var currentDirectory = workingDirectory;
        do
        {
            var foundGitDirectory = currentDirectory.GetDirectories(".git").Any();
            if (foundGitDirectory)
            {
                return new WorkingCopy(workingDirectory, gitDirectory: currentDirectory);
            }

            currentDirectory = currentDirectory.Parent;
        }
        while (currentDirectory.Parent != null);

        Exit($"Directory {workingDirectory} or any parent directory do not contain a git working copy", 3);

        return null;
    }
}
