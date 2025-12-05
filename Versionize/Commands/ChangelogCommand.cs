using McMaster.Extensions.CommandLineUtils;
using Versionize.Changelog;
using Versionize.Changelog.LinkBuilders;
using Versionize.CommandLine;
using Versionize.Config.Validation;
using Versionize.Git;
using Versionize.Lifecycle;
using Versionize.Commands;
using LibGit2Sharp;

[Command(Name = "changelog", Description = "Prints a given version's changelog to stdout")]
internal sealed class ChangelogCommand
{
    private readonly IChangelogCmdContextProvider _contextProvider;

    public ChangelogCommand(IChangelogCmdContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }

    [SemanticVersion]
    [Option(Description = "The version to include in the changelog")]
    public string? Version { get; }

    [Option(Description = "Text to display before the list of commits")]
    public string? Preamble { get; }

    public void OnExecute()
    {
        ChangelogCmdContext context = _contextProvider.GetContext(Version, Preamble);
        ChangelogCmdOptions options = context.Options;
        Repository repo = context.Repository;

        CommandLineUI.Verbosity = Versionize.CommandLine.LogLevel.Error;

        // var (FromRef, ToRef) = repo.GetCommitRange(Version, options);
        // var conventionalCommits = ConventionalCommitProvider.GetCommits(repo, options, FromRef, ToRef);
        // var linkBuilder = LinkBuilderFactory.CreateFor(repo, options.ProjectOptions.Changelog.LinkTemplates);
        // string markdown = ChangelogBuilder.GenerateCommitList(
        //     linkBuilder,
        //     conventionalCommits,
        //     options.ProjectOptions.Changelog);
        // var changelog = Preamble + markdown.TrimEnd();

        string changelog = string.Empty; // TODO: Implement changelog generation

        CommandLineUI.Verbosity = Versionize.CommandLine.LogLevel.All;

        CommandLineUI.Information(changelog);
    }
}
