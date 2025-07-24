using LibGit2Sharp;

namespace Versionize.Tests.TestSupport;

public class TestSetup(Repository repository, string workingDirectory) : IDisposable
{
    public Repository Repository { get; } = repository;
    public string WorkingDirectory { get; } = workingDirectory;

    public void Dispose()
    {
        Repository.Dispose();
        Cleanup.DeleteDirectory(WorkingDirectory);
    }

    public static TestSetup Create()
    {
        var tempDir = TempDir.Create();
        var repository = TempRepository.Create(tempDir);

        return new TestSetup(repository, tempDir);
    }
}
