using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;
using Versionize.Commands;

[Command(Name = "inspect", Description = "Prints the current version to stdout")]
internal sealed class InspectCommand
{
    private readonly IInspectCmdContextProvider _contextProvider;

    public InspectCommand(IInspectCmdContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public void OnExecute()
    {
        CommandLineUI.Verbosity = LogLevel.Error;
        InspectCmdContext context = _contextProvider.GetContext();
        CommandLineUI.Verbosity = LogLevel.All;

        CommandLineUI.Information(context.BumpFile.Version.ToNormalizedString());
    }
}
