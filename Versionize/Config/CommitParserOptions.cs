namespace Versionize.Config;

public sealed class CommitParserOptions
{
    public static readonly CommitParserOptions Default = new();

    public string[] HeaderPatterns { get; set; } = [];

    public string[] IssuesPatterns { get; set; } = [];

    public static CommitParserOptions Merge(CommitParserOptions? customOptions, CommitParserOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new CommitParserOptions
        {
            HeaderPatterns = customOptions.HeaderPatterns ?? defaultOptions.HeaderPatterns,
            IssuesPatterns = customOptions.IssuesPatterns ?? defaultOptions.IssuesPatterns,
        };
    }
}
