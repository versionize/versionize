namespace Versionize;

public class CommitParserOptions
{
    public static readonly CommitParserOptions Default = new();

    public string[] HeaderPatterns { get; set; } = Array.Empty<string>();
}
