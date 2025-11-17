using LibGit2Sharp;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Git;

namespace Versionize.Pipeline.VersionizeSteps;

public class InitWorkingCopyStep :
    IPipelineStep<EmptyResult, InitWorkingCopyStep.Options, InitWorkingCopyResult>
{
    public InitWorkingCopyResult Execute(EmptyResult input, Options options)
    {
        throw new NotImplementedException();
    }

    private static WorkingCopy? Discover(string workingDirectoryPath)
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

    public class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public static Options FromVersionizeOptions(VersionizeOptions options) => new();
    }
}
