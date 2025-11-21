using McMaster.Extensions.CommandLineUtils;

[Command(Name = "inspect", Description = "Prints the current version to stdout")]
internal sealed class InspectCommand
{
    public void OnExecute()
    {
        Console.WriteLine("Inspect command executed");
    }
}
