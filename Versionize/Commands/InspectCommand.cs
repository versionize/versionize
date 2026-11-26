using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;
using Versionize.Commands;

[Command(Name = "inspect", Description = "Prints the current version to stdout")]
internal sealed class InspectCommand(IInspectCmdContextProvider contextProvider)
{
    private readonly IInspectCmdContextProvider _contextProvider = contextProvider;

    public void OnExecute()
    {
        CommandLineUI.Verbosity = LogLevel.Error;
        InspectCmdContext context = _contextProvider.GetContext();
        CommandLineUI.Verbosity = LogLevel.All;

        var version = context.GetCurrentVersion();
        CommandLineUI.Information(version?.ToNormalizedString() ?? "");
    }
}
