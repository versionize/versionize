using LibGit2Sharp;

namespace Versionize.Tests.TestSupport;

/// <summary>
/// Helper utilities for Git operations in tests.
/// </summary>
public static class GitTestHelpers
{
    private static int CommitTimestampCounter;

    /// <summary>
    /// Stages all changes and commits them with the specified message.
    /// </summary>
    public static Commit CommitAll(IRepository repository, string message = "feat: Initial commit")
    {
        var author = GetAuthorSignature(repository);
        Commands.Stage(repository, "*");
        return repository.Commit(message, author, committer: author);
    }

    /// <summary>
    /// Gets the author signature for commits.
    /// </summary>
    private static Signature GetAuthorSignature(IRepository repository)
    {
        var user = repository.Config.Get<string>("user.name")?.Value;
        var email = repository.Config.Get<string>("user.email")?.Value;
        return new Signature(user, email, DateTime.Now.AddSeconds(CommitTimestampCounter++));
    }
}

/// <summary>
/// Helper class for creating file changes and committing them in tests.
/// </summary>
public sealed class FileCommitter(TestSetup testSetup)
{
    private readonly TestSetup _testSetup = testSetup;

    /// <summary>
    /// Creates a new file with random content and commits the change.
    /// </summary>
    /// <param name="commitMessage">The commit message to use.</param>
    /// <param name="subdirectory">Optional subdirectory to create the file in.</param>
    /// <returns>The created commit.</returns>
    public Commit CommitChange(string commitMessage, string subdirectory = "")
    {
        var fileName = Guid.NewGuid().ToString() + ".txt";
        var directory = Path.Join(_testSetup.WorkingDirectory, subdirectory);
        Directory.CreateDirectory(directory);
        var filePath = Path.Join(directory, fileName);
        File.WriteAllText(filePath, contents: Guid.NewGuid().ToString());
        return GitTestHelpers.CommitAll(_testSetup.Repository, commitMessage);
    }
}
