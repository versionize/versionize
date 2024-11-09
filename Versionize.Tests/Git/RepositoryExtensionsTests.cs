using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.BumpFiles;
using Versionize.Config;

namespace Versionize.Git;

public class RepositoryExtensionsTests : IDisposable
{
    private static int CommitTimestampCounter;
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public RepositoryExtensionsTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void SelectVersionTag_ShouldSelectLightweightTag()
    {
        var fileCommitter = new FileCommitter(_testSetup);
        var commit = fileCommitter.CommitChange("feat: Initial commit");
        _testSetup.Repository.Tags.Add("v2.0.0", commit);

        var versionTag = _testSetup.Repository.SelectVersionTag(new Version(2, 0, 0));

        versionTag.ToString().ShouldBe("refs/tags/v2.0.0");
    }

    [Fact]
    public void GetCurrentVersion_ReturnsCorrectVersionWhenTagOnlyIsTrueAndPrereleaseTagsExist()
    {
        var fileCommitter = new FileCommitter(_testSetup);

        var commit1 = fileCommitter.CommitChange("feat: commit 1");
        _testSetup.Repository.Tags.Add("v2.0.0", commit1);
        var commit2 = fileCommitter.CommitChange("feat: commit 2");
        _testSetup.Repository.Tags.Add("v2.1.0-beta.1", commit2);
        var commit3 = fileCommitter.CommitChange("feat: commit 3");
        _testSetup.Repository.Tags.Add("v2.1.0", commit3);
    
        var options = new VersionOptions { TagOnly = true, Project = ProjectOptions.DefaultOneProjectPerRepo };

        var version = _testSetup.Repository.GetCurrentVersion(options, new NullBumpFile());

        version.ShouldBe(new Version(2, 1, 0));
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    // TODO: Consider moving to a helper class
    private static Commit CommitAll(IRepository repository, string message = "feat: Initial commit")
    {
        var user = repository.Config.Get<string>("user.name").Value;
        var email = repository.Config.Get<string>("user.email").Value;
        var author = new Signature(user, email, DateTime.Now.AddMinutes(CommitTimestampCounter++));
        var committer = author;
        Commands.Stage(repository, "*");
        return repository.Commit(message, author, committer);
    }

    // TODO: Consider moving to a helper class
    class FileCommitter
    {
        private readonly TestSetup _testSetup;

        public FileCommitter(TestSetup testSetup)
        {
            _testSetup = testSetup;
        }

        public Commit CommitChange(string commitMessage, string subdirectory = "")
        {
            var fileName = Guid.NewGuid().ToString() + ".txt";
            var directory = Path.Join(_testSetup.WorkingDirectory, subdirectory);
            Directory.CreateDirectory(directory);
            var filePath = Path.Join(directory, fileName);
            File.WriteAllText(filePath, contents: "abc123");
            return CommitAll(_testSetup.Repository, commitMessage);
        }
    }
}
