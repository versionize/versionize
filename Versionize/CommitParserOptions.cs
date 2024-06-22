namespace Versionize;

public sealed class CommitParserOptions
{
    public static readonly CommitParserOptions Default = new();

    public string[] HeaderPatterns { get; set; } = [];
}
