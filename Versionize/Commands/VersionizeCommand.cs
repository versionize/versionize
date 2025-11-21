using McMaster.Extensions.CommandLineUtils;
using Versionize.Pipeline;

[Command(
    Name = "versionize",
    Description = "Automatic versioning and CHANGELOG generation, using conventional commit messages")]
[Subcommand(typeof(ChangelogCommand))]
[Subcommand(typeof(InspectCommand))]
internal sealed class VersionizeCommand
{
    private readonly IVersionizeCmdContextProvider _contextProvider;
    private readonly IInitWorkingCopyPipeline _commandPipeline;

    public VersionizeCommand(
        IVersionizeCmdContextProvider contextProvider,
        IInitWorkingCopyPipeline commandPipeline)
    {
        Console.WriteLine("VersionizeCommand constructor");
        _contextProvider = contextProvider;
        _commandPipeline = commandPipeline;
    }

    public void OnExecute()
    {
        Console.WriteLine("Versionize command executed");
        VersionizeCmdContext context = _contextProvider.GetContext();
        //_commandPipeline.InitWorkingCopy(context);
    }
}
