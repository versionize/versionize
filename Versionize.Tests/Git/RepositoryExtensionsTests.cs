using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using Shouldly;
using Versionize.BumpFiles;
using Versionize.Config;
using LibGit2Sharp;
using NSubstitute;
using NuGet.Versioning;

namespace Versionize.Git;

public class RepositoryExtensionsTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public RepositoryExtensionsTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void DummyTest()
    {
        var tagCollection = Substitute.For<TagCollection>();
        var tag1 = Substitute.For<Tag>();
        var target = Substitute.For<GitObject>();
        tag1.Target.Returns(target);
        var id = new ObjectId("a1b2c3d4e5f60123456789012345678901234567");
        target.Id.Returns(id);
        tag1.FriendlyName.Returns("v1.0.0");
        var tags = new List<Tag> { tag1 };
        tagCollection.GetEnumerator().Returns(tags.GetEnumerator());
        var repository = Substitute.For<IRepository>();
        repository.Tags.Returns(tagCollection);
        repository.Tags.Count().ShouldBe(1);
        foreach (var tag in repository.Tags)
        {
            tag.FriendlyName.ShouldBe("v1.0.0");
        }

        var gitConfig = Substitute.For<Configuration>();
        repository.Config.Returns(gitConfig);
        var configEntry = Substitute.For<ConfigurationEntry<string>>();
        configEntry.Key.Returns("user.name");
        configEntry.Value.Returns("Test User");
        gitConfig.Get<string>("user.name").Returns(configEntry);
        var configEntryEmail = Substitute.For<ConfigurationEntry<string>>();
        configEntryEmail.Key.Returns("user.email");
        configEntryEmail.Value.Returns("testuser@example.com");
        gitConfig.Get<string>("user.email").Returns(configEntryEmail);

        repository.Config.Get<string>("user.name").Value.ShouldBe("Test User");
        repository.Config.Get<string>("user.email").Value.ShouldBe("testuser@example.com");

        var filter = new CommitFilter();
        filter.SortBy = CommitSortStrategies.Time | CommitSortStrategies.Topological;
        var commits = Substitute.For<IQueryableCommitLog>();
        var commit = Substitute.For<Commit>();
        var logEntry = new LogEntry();
        typeof(LogEntry).GetProperty("Commit").SetValue(logEntry, commit);
        typeof(LogEntry).GetProperty("Path").SetValue(logEntry, "/abc");
        IEnumerable<LogEntry> logEntries = [logEntry];
        commits.QueryBy(Arg.Any<string>(), filter).Returns(logEntries);
        repository.Commits.Returns(commits);

        // assert
        var result = repository.Commits.QueryBy("/abc", filter);
        result.Count().ShouldBe(1);
        result.First().Commit.ShouldBe(commit);
        result.First().Path.ShouldBe("/abc");

        var status = Substitute.For<RepositoryStatus>();
        status.IsDirty.Returns(true);
        var statusEntry = Substitute.For<StatusEntry>();
        statusEntry.FilePath.Returns("file1.txt");
        status.GetEnumerator().Returns(new List<StatusEntry> { statusEntry }.GetEnumerator());
        var statusOptions = new StatusOptions { IncludeUntracked = false };
        repository.RetrieveStatus(statusOptions).Returns(status);

        // assert
        repository.RetrieveStatus(statusOptions).IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void ChangeCommitterTest1()
    {
        var repoBuilder = GitRepositoryBuilder.Create();
        var repository = repoBuilder.Build();
        // ChangeCommitter.CreateCommit(
        //     repository,
        //     new ChangeCommitter.Options
        //     {
        //         SkipCommit = true,
        //         DryRun = false,
        //         Sign = false,
        //         WorkingDirectory = "",
        //     },
        //     new SemanticVersion(1, 0, 0),
        //     NullBumpFile.Default,
        //     null);
    }

    [Fact]
    public void SelectVersionTag_ShouldSelectLightweightTag()
    {
        // Arrange
        var fileCommitter = new FileCommitter(_testSetup);
        var commit = fileCommitter.CommitChange("feat: Initial commit");
        _testSetup.Repository.Tags.Add("v2.0.0", commit);

        // Act
        var versionTag = _testSetup.Repository.SelectVersionTag(
            new SemanticVersion(2, 0, 0),
            ProjectOptions.DefaultOneProjectPerRepo);

        // Assert
        versionTag.ToString().ShouldBe("refs/tags/v2.0.0");
    }

    // [Fact]
    // public void GetCurrentVersion_ReturnsCorrectVersion_When_TagOnlyIsTrueAndPrereleaseTagsExist()
    // {
    //     // Arrange
    //     var fileCommitter = new FileCommitter(_testSetup);

    //     var commit1 = fileCommitter.CommitChange("feat: commit 1");
    //     _testSetup.Repository.Tags.Add("v2.0.0", commit1);
    //     var commit2 = fileCommitter.CommitChange("feat: commit 2");
    //     _testSetup.Repository.Tags.Add("v2.1.0-beta.1", commit2);
    //     var commit3 = fileCommitter.CommitChange("feat: commit 3");
    //     _testSetup.Repository.Tags.Add("v2.1.0", commit3);

    //     var options = new VersionOptions { SkipBumpFile = true, Project = ProjectOptions.DefaultOneProjectPerRepo };

    //     // Act
    //     var version = _testSetup.Repository.GetCurrentVersion(options, NullBumpFile.Default);

    //     // Assert
    //     version.ShouldBe(new SemanticVersion(2, 1, 0));
    // }

    [Fact]
    public void VersionTagsExists_ShouldReturnTrue_When_TagExists()
    {
        // Arrange
        var fileCommitter = new FileCommitter(_testSetup);
        var commit = fileCommitter.CommitChange("feat: Initial commit");
        _testSetup.Repository.Tags.Add("v1.0.0", commit);

        // Act
        var exists = _testSetup.Repository.VersionTagsExists(
            new SemanticVersion(1, 0, 0),
            ProjectOptions.DefaultOneProjectPerRepo);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public void VersionTagsExists_ShouldReturnFalse_When_TagDoesNotExist()
    {
        // Arrange
        var fileCommitter = new FileCommitter(_testSetup);
        var commit = fileCommitter.CommitChange("feat: Initial commit");
        _testSetup.Repository.Tags.Add("v1.0.0", commit);

        // Act
        var exists = _testSetup.Repository.VersionTagsExists(
            new SemanticVersion(1, 0, 0, "alpha.1"),
            ProjectOptions.DefaultOneProjectPerRepo);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public void GetCommits_ShouldReturnAllCommits_When_NoFilterIsProvided()
    {
        // Arrange
        var fileCommitter = new FileCommitter(_testSetup);
        fileCommitter.CommitChange("feat: commit 1");
        fileCommitter.CommitChange("fix: commit 2");

        // Act
        var commits = _testSetup.Repository.GetCommits(
            ProjectOptions.DefaultOneProjectPerRepo,
            filter: null);

        // Assert
        commits.Count().ShouldBe(2);
    }

    [Fact]
    public void GetCommits_ShouldOnlyReturnFirstParentCommits_When_FirstParentOnlyIsTrue()
    {
        // Arrange
        var fileCommitter = new FileCommitter(_testSetup);

        // Create initial commit on main
        var commit1 = fileCommitter.CommitChange("feat: commit 1 on main");

        // Create a feature branch and add commits to it
        var featureBranch = _testSetup.Repository.CreateBranch("feature-branch");
        LibGit2Sharp.Commands.Checkout(_testSetup.Repository, featureBranch);

        var commit2 = fileCommitter.CommitChange("feat: commit 2 on feature");
        var commit3 = fileCommitter.CommitChange("feat: commit 3 on feature");

        // Switch back to main and create another commit
        LibGit2Sharp.Commands.Checkout(_testSetup.Repository, "master"); // or "main" depending on default branch name
        var commit4 = fileCommitter.CommitChange("feat: commit 4 on main");

        // Merge the feature branch into main (creates a merge commit)
        var signature = _testSetup.Repository.Config.BuildSignature(DateTimeOffset.Now);
        var merge = _testSetup.Repository.Merge(featureBranch, signature, new MergeOptions());

        var commit5 = fileCommitter.CommitChange("feat: commit 5 on main");

        // Act - with FirstParentOnly = true
        var filterFirstParent = new CommitFilter
        {
            //FirstParentOnly = true,
        };
        var commitsFirstParent = _testSetup.Repository.GetCommits(
            ProjectOptions.DefaultOneProjectPerRepo,
            filter: filterFirstParent);

        // Assert
        commitsFirstParent.Count().ShouldBe(3);
        commitsFirstParent.ShouldContain(merge.Commit);
        commitsFirstParent.ShouldContain(commit1);
        commitsFirstParent.ShouldNotContain(commit2);
        commitsFirstParent.ShouldNotContain(commit3);
        commitsFirstParent.ShouldContain(commit4);
    }

    [Fact]
    public void GetCommitsSinceLastVersion_ShouldReturnCommits_When_ProjectPathIsEmpty()
    {
        // Arrange
        var fileCommitter = new FileCommitter(_testSetup);
        var commit1 = fileCommitter.CommitChange("feat: commit 1");
        var commit2 = fileCommitter.CommitChange("fix: commit 2");
        var commit3 = fileCommitter.CommitChange("chore: commit 3");
        var tag1 = _testSetup.Repository.Tags.Add("v1.0.0", commit1);
        var tag2 = _testSetup.Repository.Tags.Add("v1.1.0", commit2);
        var tag3 = _testSetup.Repository.Tags.Add("v1.2.0", commit3);

        // Act
        var commits = _testSetup.Repository.GetCommitsSinceRef(
            tag3.Target,
            ProjectOptions.DefaultOneProjectPerRepo,
            filter: null);

        // Assert
        commits.Count().ShouldBe(0);

        // Act
        commits = _testSetup.Repository.GetCommitsSinceRef(
            tag2.Target,
            ProjectOptions.DefaultOneProjectPerRepo,
            filter: null);

        // Assert
        commits.Count().ShouldBe(1);
        commits.ElementAt(0).ShouldBe(commit3);

        // Act
        commits = _testSetup.Repository.GetCommitsSinceRef(
            tag1.Target,
            ProjectOptions.DefaultOneProjectPerRepo,
            filter: null);

        // Assert
        commits.Count().ShouldBe(2);
        commits.ElementAt(0).ShouldBe(commit3);
        commits.ElementAt(1).ShouldBe(commit2);
    }

    // TODO: GetCommitRange, GetPreviousVersion, IsConfiguredForCommits

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
