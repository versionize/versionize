using System.Runtime.CompilerServices;
using LibGit2Sharp;
using Versionize.CommandLine;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Versionize.Git;

internal interface IRepositoryProvider
{
    Repository GetRepository(string workingDirectory);
}

internal sealed class RepositoryProvider : IRepositoryProvider
{
    public Repository GetRepository(string workingDirectory)
    {
        var gitDirectory = FindGitDirectory(workingDirectory);
        var repository = new Repository(gitDirectory);
        return repository;
    }

    /// <summary>
    /// Searches for the root directory of a Git repository by traversing up the directory tree
    /// until it finds a .git directory or reaches the root of the file system.
    /// </summary>
    private static string FindGitDirectory(string workingDirectoryPath)
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
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        } while (currentDirectory is { Parent: not null });

        throw new VersionizeException(ErrorMessages.RepositoryNotGit(workingDirectory.FullName), 3);
    }
}
