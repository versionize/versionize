using McMaster.Extensions.CommandLineUtils;
using Versionize.Config.Validation;

[Command(Name = "changelog", Description = "Prints a given version's changelog to stdout")]
internal sealed class ChangelogCommand
{
    [Option(Description = "The version to include in the changelog")]
    [SemanticVersion]
    public string? Version { get; }

    [Option(Description = "Text to display before the list of commits")]
    public string? Preamble { get; }

    // var versionOption = changelogCmd.Option(
    //     "-v|--version <VERSION>",
    //     "The version to include in the changelog",
    //     CommandOptionType.SingleValue)
    //     .Accepts(v => v.Use(SemanticVersionValidator.Default));

    // var preambleOption = changelogCmd.Option(
    //     "-p|--preamble <PREAMBLE>",
    //     "Text to display before the list of commits",
    //     CommandOptionType.SingleValue);

    public ChangelogCommand()
    {
    }

    public void OnExecute()
    {
        Console.WriteLine("Changelog command executed");
    }
}
