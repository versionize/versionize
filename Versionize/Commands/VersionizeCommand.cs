using McMaster.Extensions.CommandLineUtils;
using Versionize.Git;
using Versionize.Lifecycle;
using Versionize.Commands;
using Versionize.BumpFiles;

[Command(
    Name = "versionize",
    Description = "Automatic versioning and CHANGELOG generation, using conventional commit messages")]
[Subcommand(typeof(ChangelogCommand))]
[Subcommand(typeof(InspectCommand))]
internal sealed class VersionizeCommand
{
    private readonly IVersionizeCmdContextProvider _contextProvider;

    public VersionizeCommand(
        IVersionizeCmdContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public void OnExecute()
    {
        VersionizeCmdContext context = _contextProvider.GetContext();
        var options = context.Options;
        var repo = context.Repository;

        var bumpFile = BumpFileProvider.GetBumpFile(options);
        var version = repo.GetCurrentVersion(options, bumpFile);

        // Parse commit messages
        var (isInitialRelease, conventionalCommits) = ConventionalCommitProvider.GetCommits(repo, options, version);

        // Bump version
        var newVersion = VersionCalculator.Bump(options, version, isInitialRelease, conventionalCommits);

        // Update bump file
        BumpFileUpdater.Update(options, newVersion, bumpFile);

        // Update changelog
        var changelog = ChangelogUpdater.Update(repo, options, newVersion, version, conventionalCommits);

        // Commit
        ChangeCommitter.CreateCommit(repo, options, newVersion, bumpFile, changelog);

        // Tag
        ReleaseTagger.CreateTag(repo, options, newVersion);
    }
}
