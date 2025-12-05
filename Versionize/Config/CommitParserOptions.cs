namespace Versionize.Config;

public sealed class CommitParserOptions
{
    public static readonly CommitParserOptions Default = new();

    public string[] HeaderPatterns { get; init; } = [];

    public string[] IssuesPatterns { get; init; } = [];

    public static CommitParserOptions MergeWithDefault(CommitParserOptions? customOptions)
    {
        if (customOptions == null)
        {
            return Default;
        }

        return new CommitParserOptions
        {
            HeaderPatterns = customOptions.HeaderPatterns ?? Default.HeaderPatterns,
            IssuesPatterns = customOptions.IssuesPatterns ?? Default.IssuesPatterns,
        };
    }
}
