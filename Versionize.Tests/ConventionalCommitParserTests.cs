using LibGit2Sharp;
using Shouldly;
using Xunit;

namespace Versionize.Tests;

public class ConventionalCommitParserTests
{
    [Fact]
    public void ShouldParseTypeScopeAndSubjectFromSingleLineCommitMessage()
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "feat(scope): broadcast $destroy event on scope destruction");
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        Assert.Equal("feat", conventionalCommit.Type);
        Assert.Equal("scope", conventionalCommit.Scope);
        Assert.Equal("broadcast $destroy event on scope destruction", conventionalCommit.Subject);
    }

    [Fact]
    public void ShouldUseFullHeaderAsSubjectIfNoTypeWasGiven()
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "broadcast $destroy event on scope destruction");
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        Assert.Equal(testCommit.Message, conventionalCommit.Subject);
    }

    [Fact]
    public void ShouldUseFullHeaderAsSubjectIfNoTypeWasGivenButSubjectUsesColon()
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "broadcast $destroy event: on scope destruction");
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        Assert.Equal(testCommit.Message, conventionalCommit.Subject);
    }

    [Fact]
    public void ShouldParseTypeScopeAndSubjectFromSingleLineCommitMessageIfSubjectUsesColon()
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "feat(scope): broadcast $destroy: event on scope destruction");
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        Assert.Equal("feat", conventionalCommit.Type);
        Assert.Equal("scope", conventionalCommit.Scope);
        Assert.Equal("broadcast $destroy: event on scope destruction", conventionalCommit.Subject);
    }

    [Fact]
    public void ShouldExtractCommitNotes()
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", "feat(scope): broadcast $destroy: event on scope destruction\nBREAKING CHANGE: this will break rc1 compatibility");
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        Assert.Single(conventionalCommit.Notes);

        var breakingChangeNote = conventionalCommit.Notes.Single();

        Assert.Equal("BREAKING CHANGE", breakingChangeNote.Title);
        Assert.Equal("this will break rc1 compatibility", breakingChangeNote.Text);
    }

    [Theory]
    [InlineData("feat!: broadcast $destroy: event on scope destruction")]
    [InlineData("feat(scope)!: broadcast $destroy: event on scope destruction")]
    public void ShouldSupportExclamationMarkToSignifyingBreakingChanges(string commitMessage)
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", commitMessage);
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        conventionalCommit.Notes.ShouldHaveSingleItem();
        conventionalCommit.Notes[0].Title.ShouldBe("BREAKING CHANGE");
        conventionalCommit.Notes[0].Text.ShouldBe(string.Empty);
    }
    
    [Theory]
    [InlineData("fix: subject text #64", new[] { "64" })]
    [InlineData("fix: subject #64 text", new[] { "64" })]
    [InlineData("fix: #64 subject text", new[] { "64" })]
    [InlineData("fix: subject text. #64 #65", new[] { "64", "65" })]
    [InlineData("fix: subject text. (#64) (#65)", new[] { "64", "65" })]
    [InlineData("fix: subject text. #64#65", new[] { "64", "65" })]
    [InlineData("fix: #64 subject #65 text. (#66)", new[] { "64", "65", "66" })]
    public void ShouldExtractCommitIssues(string commitMessage, string[] expectedIssues)
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", commitMessage);
        var conventionalCommit = ConventionalCommitParser.Parse(testCommit);

        Assert.Equal(conventionalCommit.Issues.Count, expectedIssues.Length);

        foreach (var expectedIssue in expectedIssues)
        {
            var issue = conventionalCommit.Issues.SingleOrDefault(x => x.Id == expectedIssue);
            Assert.NotNull(issue);
            Assert.Equal(issue.Token, $"#{expectedIssue}");
        }
    }

    [Theory]
    [InlineData("fix: subject text #64", "", "fix", "subject text #64")]
    [InlineData("feat(scope): subject text", "scope", "feat", "subject text")]
    [InlineData("Merged PR 123: fix: subject text #64", "", "fix", "subject text #64")]
    [InlineData("Merged PR 321: feat(scope): subject text", "scope", "feat", "subject text")]
    [InlineData("Pull Request 11792: fix: subject text #64", "", "fix", "subject text #64")]
    [InlineData("Pull Request 11792: feat(scope): subject text", "scope", "feat", "subject text")]
    public void ShouldParseCommitWithExtraHeaderPatterns(string commitMessage,
        string scope, string type, string subject)
    {
        var testCommit = new TestCommit("c360d6a307909c6e571b29d4a329fd786c5d4543", commitMessage);
        var conventionalCommit = ConventionalCommitParser.Parse(
            testCommit,
            new CommitParserOptions
            {
                HeaderPatterns = new []
                {
                    "^Merged PR \\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$",
                    "^Pull Request \\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$"
                }
            });

        Assert.Equal(conventionalCommit.Scope, scope);
        Assert.Equal(conventionalCommit.Type, type);
        Assert.Equal(conventionalCommit.Subject, subject);
    }
}

public class TestCommit : Commit
{
    private readonly string _sha;
    private readonly string _message;

    public TestCommit(string sha, string message)
    {
        _sha = sha;
        _message = message;
    }

    public override string Message { get => _message; }

    public override string Sha { get => _sha; }
}
