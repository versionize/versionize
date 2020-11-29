using System;
using LibGit2Sharp;

namespace Versionize.Tests.TestSupport
{
    public class TestSetup : IDisposable
    {
        public Repository Repository { get; }
        public string WorkingDirectory { get; }

        public TestSetup(Repository repository, string workingDirectory)
        {
            Repository = repository;
            WorkingDirectory = workingDirectory;
        }

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
}