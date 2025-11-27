using McMaster.Extensions.CommandLineUtils;
using Versionize.BumpFiles;
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
        InspectCmdContext context = _contextProvider.GetContext();

        CommandLineUI.Verbosity = LogLevel.Error;
        IBumpFile bumpFile = BumpFileProvider.GetBumpFile(context.Options);
        CommandLineUI.Verbosity = LogLevel.All;

        CommandLineUI.Information(bumpFile.Version.ToNormalizedString());
    }
}
