using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using Shouldly;
using Version = NuGet.Versioning.SemanticVersion;
using Versionize.Git;

namespace Versionize.Tests;

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
    public void ShouldSelectLightweight()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "2.0.0");
        var commit = CommitAll(_testSetup.Repository);

        _testSetup.Repository.Tags.Add("v2.0.0", commit);

        var versionTag = _testSetup.Repository.SelectVersionTag(new Version(2, 0, 0));

        versionTag.ToString().ShouldBe("refs/tags/v2.0.0");
    }

    [Fact]
    public void ShouldSelectAnnotatedTags()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "2.0.0");
        var commit = CommitAll(_testSetup.Repository);

        _testSetup.Repository.Tags.Add("v2.0.0", commit, GetAuthorSignature(), "Some annotation message without a version included");

        var versionTag = _testSetup.Repository.SelectVersionTag(new Version(2, 0, 0));

        versionTag.ToString().ShouldBe("refs/tags/v2.0.0");
    }

    [Fact]
    public void ShouldVerifyThatTagNamesStartWith_v_Prefix()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "2.0.0");
        var commit = CommitAll(_testSetup.Repository);

        var tag = _testSetup.Repository.Tags.Add("2.0.0", commit, GetAuthorSignature(), "Some annotation message without a version included");

        tag.IsSemanticVersionTag().ShouldBeFalse();
    }

    [Fact]
    public void ShouldVerifyThatSemanticVersionTagCanBeParsed()
    {
        TempProject.CreateCsharpProject(Path.Join(_testSetup.WorkingDirectory, "project1"), "2.0.0");
        var commit = CommitAll(_testSetup.Repository);

        var tag = _testSetup.Repository.Tags.Add("vNext", commit, GetAuthorSignature(), "Some annotation message without a version included");

        tag.IsSemanticVersionTag().ShouldBeFalse();
    }

    private static Commit CommitAll(IRepository repository, string message = "feat: Initial commit")
    {
        var author = GetAuthorSignature();
        Commands.Stage(repository, "*");
        return repository.Commit(message, author, author);
    }

    private static Signature GetAuthorSignature()
    {
        return new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
