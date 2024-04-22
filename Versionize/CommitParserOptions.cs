namespace Versionize;

public class CommitParserOptions
{
    public static CommitParserOptions Default = new();

    public string[] HeaderPatterns { get; set; } = Array.Empty<string>();
}
