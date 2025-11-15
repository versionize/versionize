using System.Text.Json;
using Versionize.CommandLine;

namespace Versionize.Config;

public sealed class FileConfig
{
    /// <summary>
    /// Suppresses all console output when enabled.
    /// </summary>
    public bool? Silent { get; init; }

    /// <summary>
    /// Performs a trial run without making any actual changes to files, commits, or tags.
    /// Useful for previewing what versionize would do without modifying the repository.
    /// </summary>
    public bool? DryRun { get; init; }

    /// <summary>
    /// Manually specifies the version to release instead of calculating it from conventional commits.
    /// Must be a valid semantic version (e.g., "1.2.3", "2.0.0-beta.1").
    /// </summary>
    public string? ReleaseAs { get; init; }

    /// <summary>
    /// Allows versionize to run even when there are uncommitted changes in the working directory.
    /// By default, versionize requires a clean working directory.
    /// </summary>
    public bool? SkipDirty { get; init; }

    /// <summary>
    /// Prevents creating a git commit after updating the changelog and version files.
    /// The changes will remain staged but uncommitted.
    /// </summary>
    public bool? SkipCommit { get; init; }

    /// <summary>
    /// Prevents creating a git tag after making the release commit.
    /// Useful when you want to create commits but defer tagging until later.
    /// </summary>
    public bool? SkipTag { get; init; }

    /// <summary>
    /// Skips updating the CHANGELOG.md file during the release process.
    /// Version bumping and commits will still occur unless also disabled.
    /// </summary>
    public bool? SkipChangelog { get; init; }

    /// <summary>
    /// Read version information from git tags instead of project files, and don't update project files.
    /// </summary>
    public bool? TagOnly { get; init; }

    /// <summary>
    /// Prevents version bumping when no significant commits (fix, feat, or BREAKING CHANGE) are found.
    /// Without this option, insignificant commits would still trigger a patch version bump.
    /// </summary>
    public bool? IgnoreInsignificantCommits { get; init; }

    /// <summary>
    /// Causes versionize to exit with a non-zero exit code when no significant commits are found.
    /// Useful in CI/CD pipelines to to bail out when no release is needed.
    /// </summary>
    public bool? ExitInsignificantCommits { get; init; }

    /// <summary>
    /// Appends text to the end of the release commit message.
    /// Commonly used to add CI skip directives like "[skip ci]" or "[ci skip]".
    /// </summary>
    public string? CommitSuffix { get; init; }

    /// <summary>
    /// Creates a prerelease version with the specified identifier (e.g., "alpha", "beta", "rc").
    /// The version will be formatted as "major.minor.patch-identifier.number".
    /// </summary>
    public string? Prerelease { get; init; }

    /// <summary>
    /// When updating the changelog, include all commits since the last full version.
    /// If false, only includes commits since the last pre-release version.
    /// </summary>
    public bool? AggregatePrereleases { get; init; }

    /// <summary>
    /// Only considers commits on the main branch line, ignoring commits from merged branches.
    /// When enabled, merge commits are included but the individual commits within merged branches are excluded.
    /// Useful for keeping changelogs focused on direct branch commits and avoiding duplicate entries from feature branches.
    /// </summary>
    public bool? FirstParentOnlyCommits { get; init; }

    /// <summary>
    /// Cryptographically signs the release commit and tag using GPG.
    /// Requires Git to be configured with a signing key.
    /// </summary>
    public bool? Sign { get; init; }

    /// <summary>
    /// Defines the format for git tags using placeholders {name} and {version}.
    /// Examples: "v{version}" produces "v1.2.3", "{name}/v{version}" produces "myproject/v1.2.3".
    /// Essential for monorepo scenarios where multiple projects need distinct tag patterns.
    /// </summary>
    public string? TagTemplate { get; init; }

    /// <summary>
    /// Configuration for parsing conventional commit messages, including custom header patterns and note keywords.
    /// </summary>
    public CommitParserOptions? CommitParser { get; init; }

    /// <summary>
    /// Defines multiple projects for monorepo support, each with its own versioning configuration.
    /// Each project can have a unique name, path, tag template, and changelog options.
    /// </summary>
    public ProjectOptions[] Projects { get; init; } = [];

    /// <summary>
    /// Configuration for changelog generation, including sections, link builders, and templates.
    /// </summary>
    public ChangelogOptions? Changelog { get; init; }

    public static FileConfig? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FileConfig>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception e)
        {
            throw new VersionizeException(ErrorMessages.FailedToParseVersionizeFile(e.Message), 1);
        }
    }
}
