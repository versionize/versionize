using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.Git;
using Versionize.Config;
using LibGit2Sharp;

namespace Versionize.Tests;

public class ReleaseTaggerTests : IDisposable
{
    private readonly TestSetup _testSetup;

    public ReleaseTaggerTests()
    {
        _testSetup = TestSetup.Create();
        CommandLineUI.Platform = new TestPlatformAbstractions();
    }

    [Fact]
    public void DoesntCreateATag_When_DryRunIsTrueAndSkipTagIsFalse()
    {
        var options = new ReleaseTagger.Options
        {
            DryRun = true,
            SkipTag = false,
            Sign = false,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        _testSetup.Repository.Tags.Count().ShouldBe(0);
    }

    [Fact]
    public void DoesntCreateATag_When_DryRunIsFalseAndSkipTagIsTrue()
    {
        var options = new ReleaseTagger.Options
        {
            DryRun = false,
            SkipTag = true,
            Sign = false,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        _testSetup.Repository.Tags.Count().ShouldBe(0);
    }

    [Fact]
    public void CreatesATag_When_DryRunIsFalseAndSkipTagIsFalse()
    {
        var options = new ReleaseTagger.Options
        {
            DryRun = false,
            SkipTag = false,
            Sign = false,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var fileCommitter = new FileCommitter(_testSetup);
        fileCommitter.CommitChange("feat: initial commit");

        _testSetup.Repository.Commits.Count().ShouldBe(1);

        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        _testSetup.Repository.Tags.Count().ShouldBe(1);
        var tag = _testSetup.Repository.Tags.Single();
        tag.FriendlyName.ShouldBe("v1.2.3");

        GitProcessUtil.IsTagSigned(_testSetup.WorkingDirectory, tag).ShouldBeFalse();
    }

    [Fact]
    public void CreatesASignedTag_When_DryRunIsFalseAndSkipTagIsFalseAndSignIsTrue()
    {
        var gpgFilePath = "./TestKeyForGpgSigning.pgp";
        GitProcessUtil.RunGpgCommand($"--import \"{gpgFilePath}\"");
        _testSetup.Repository.Config.Set("user.signingkey", "0C79B0FDFF00BDF6");

        var options = new ReleaseTagger.Options
        {
            DryRun = false,
            SkipTag = false,
            Sign = true,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = _testSetup.WorkingDirectory,
        };

        var fileCommitter = new FileCommitter(_testSetup);
        fileCommitter.CommitChange("feat: initial commit");

        _testSetup.Repository.Commits.Count().ShouldBe(1);

        ReleaseTagger.CreateTag(
            _testSetup.Repository,
            options,
            new Version(1, 2, 3));

        _testSetup.Repository.Tags.Count().ShouldBe(1);
        var tag = _testSetup.Repository.Tags.Single();
        tag.FriendlyName.ShouldBe("v1.2.3");

        GitProcessUtil.IsTagSigned(_testSetup.WorkingDirectory, tag).ShouldBeTrue();
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    private static void CommitAll(IRepository repository, string message = "feat: Initial commit")
    {
        var user = repository.Config.Get<string>("user.name").Value;
        var email = repository.Config.Get<string>("user.email").Value;
        var author = new Signature(user, email, DateTime.Now);
        var committer = author;
        Commands.Stage(repository, "*");
        repository.Commit(message, author, committer);
    }

    class FileCommitter
    {
        private readonly TestSetup _testSetup;

        public FileCommitter(TestSetup testSetup)
        {
            _testSetup = testSetup;
        }

        public void CommitChange(string commitMessage, string subdirectory = "")
        {
            var fileName = Guid.NewGuid().ToString() + ".txt";
            var directory = Path.Join(_testSetup.WorkingDirectory, subdirectory);
            Directory.CreateDirectory(directory);
            var filePath = Path.Join(directory, fileName);
            File.WriteAllText(filePath, contents: "abc123");
            CommitAll(_testSetup.Repository, commitMessage);
        }
    }
}
