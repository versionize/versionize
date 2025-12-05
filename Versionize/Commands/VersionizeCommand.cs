using McMaster.Extensions.CommandLineUtils;
using Versionize.Commands;

[Command(
    Name = "versionize",
    Description = "Automatic versioning and CHANGELOG generation, using conventional commit messages")]
[Subcommand(typeof(ChangelogCommand))]
[Subcommand(typeof(InspectCommand))]
internal sealed class VersionizeCommand
{
    private readonly IVersionizeCmdContextProvider _contextProvider;
    private readonly IVersionizeCmdPipeline _commandPipeline;

    public VersionizeCommand(
        IVersionizeCmdContextProvider contextProvider,
        IVersionizeCmdPipeline commandPipeline)
    {
        _contextProvider = contextProvider;
        _commandPipeline = commandPipeline;
    }

    public void OnExecute()
    {
        VersionizeCmdContext context = _contextProvider.GetContext();

        _commandPipeline.Begin(context)
            .ParseCommitsSinceLastVersion()
            .BumpVersion()
            .UpdateBumpFile()
            .UpdateChangelog()
            .CreateCommit()
            .CreateTag();
    }
}
