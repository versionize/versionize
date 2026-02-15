using System.Text.Json;
using Versionize.CommandLine;

namespace Versionize.Config;

public static class FileConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly HttpClient HttpClient = new();

    public static FileConfig? LoadMerged(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var source = ConfigSource.FromFile(filePath);
        return LoadMerged(source);
    }

    private static FileConfig LoadMerged(ConfigSource source)
    {
        var config = LoadConfig(source);
        if (string.IsNullOrWhiteSpace(config.Extends))
        {
            return config;
        }

        var parentSource = ResolveSource(source, config.Extends);
        var parentConfig = LoadMerged(parentSource);
        return Merge(parentConfig, config);
    }

    private static FileConfig LoadConfig(ConfigSource source)
    {
        FileConfig? config;

        try
        {
            var json = source.IsUrl
                ? HttpClient.GetStringAsync(source.Location).GetAwaiter().GetResult()
                : File.ReadAllText(source.Location);

            config = JsonSerializer.Deserialize<FileConfig>(json, JsonOptions);
        }
        catch (Exception e)
        {
            if (source.IsUrl)
            {
                throw new VersionizeException(ErrorMessages.FailedToLoadExtendedConfig(source.Location, e.Message), 1);
            }

            throw new VersionizeException(ErrorMessages.FailedToParseVersionizeFile(e.Message), 1);
        }

        if (config == null)
        {
            throw new VersionizeException(ErrorMessages.FailedToParseVersionizeFile("empty configuration"), 1);
        }

        return config;
    }

    private static ConfigSource ResolveSource(ConfigSource current, string reference)
    {
        var trimmed = reference.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme is not ("http" or "https"))
            {
                throw new VersionizeException(ErrorMessages.ExtendedConfigInvalidScheme(trimmed), 1);
            }

            return ConfigSource.FromUrl(uri.ToString());
        }

        if (current.IsUrl)
        {
            if (current.BaseUri == null)
            {
                throw new VersionizeException(ErrorMessages.FailedToLoadExtendedConfig(trimmed, "Missing base URL"), 1);
            }

            var combined = new Uri(current.BaseUri, trimmed);
            if (combined.Scheme is not ("http" or "https"))
            {
                throw new VersionizeException(ErrorMessages.ExtendedConfigInvalidScheme(combined.ToString()), 1);
            }

            return ConfigSource.FromUrl(combined.ToString());
        }

        var baseDirectory = current.Directory ?? Directory.GetCurrentDirectory();
        var fullPath = Path.GetFullPath(Path.IsPathRooted(trimmed)
            ? trimmed
            : Path.Combine(baseDirectory, trimmed));

        if (!File.Exists(fullPath))
        {
            throw new VersionizeException(ErrorMessages.ExtendedConfigFileNotFound(fullPath), 1);
        }

        return ConfigSource.FromFile(fullPath);
    }

    private static FileConfig Merge(FileConfig baseConfig, FileConfig overrideConfig)
    {
        return new FileConfig
        {
            Extends = overrideConfig.Extends,
            Silent = overrideConfig.Silent ?? baseConfig.Silent,
            DryRun = overrideConfig.DryRun ?? baseConfig.DryRun,
            ReleaseAs = overrideConfig.ReleaseAs ?? baseConfig.ReleaseAs,
            SkipDirty = overrideConfig.SkipDirty ?? baseConfig.SkipDirty,
            SkipCommit = overrideConfig.SkipCommit ?? baseConfig.SkipCommit,
            SkipTag = overrideConfig.SkipTag ?? baseConfig.SkipTag,
            SkipChangelog = overrideConfig.SkipChangelog ?? baseConfig.SkipChangelog,
            TagOnly = overrideConfig.TagOnly ?? baseConfig.TagOnly,
            IgnoreInsignificantCommits = overrideConfig.IgnoreInsignificantCommits ?? baseConfig.IgnoreInsignificantCommits,
            ExitInsignificantCommits = overrideConfig.ExitInsignificantCommits ?? baseConfig.ExitInsignificantCommits,
            CommitSuffix = overrideConfig.CommitSuffix ?? baseConfig.CommitSuffix,
            Prerelease = overrideConfig.Prerelease ?? baseConfig.Prerelease,
            AggregatePrereleases = overrideConfig.AggregatePrereleases ?? baseConfig.AggregatePrereleases,
            FirstParentOnlyCommits = overrideConfig.FirstParentOnlyCommits ?? baseConfig.FirstParentOnlyCommits,
            Sign = overrideConfig.Sign ?? baseConfig.Sign,
            TagTemplate = overrideConfig.TagTemplate ?? baseConfig.TagTemplate,
            CommitParser = MergeCommitParser(baseConfig.CommitParser, overrideConfig.CommitParser),
            Projects = MergeProjects(baseConfig.Projects, overrideConfig.Projects),
            Changelog = MergeChangelog(baseConfig.Changelog, overrideConfig.Changelog),
        };
    }

    private static CommitParserOptions? MergeCommitParser(CommitParserOptions? baseOptions, CommitParserOptions? overrideOptions)
    {
        if (baseOptions == null)
        {
            return overrideOptions;
        }

        if (overrideOptions == null)
        {
            return baseOptions;
        }

        return new CommitParserOptions
        {
            HeaderPatterns = overrideOptions.HeaderPatterns ?? baseOptions.HeaderPatterns,
            IssuesPatterns = overrideOptions.IssuesPatterns ?? baseOptions.IssuesPatterns,
        };
    }

    // Shallow merge for projects simply concatenates the project arrays. This allows adding new projects in the local config without needing to repeat the base config projects.
    private static ProjectOptions[] MergeProjects(ProjectOptions[]? baseProjects, ProjectOptions[]? overrideProjects)
    {
        return [.. baseProjects ?? [], .. overrideProjects ?? []];
    }

    private static ChangelogOptions? MergeChangelog(ChangelogOptions? baseOptions, ChangelogOptions? overrideOptions)
    {
        if (baseOptions == null)
        {
            return overrideOptions;
        }

        if (overrideOptions == null)
        {
            return baseOptions;
        }

        return ChangelogOptions.Merge(overrideOptions, baseOptions);
    }

    private sealed record ConfigSource(string Location, bool IsUrl, string? Directory, Uri? BaseUri)
    {
        public static ConfigSource FromFile(string path)
        {
            var fullPath = Path.GetFullPath(path);
            return new ConfigSource(fullPath, false, Path.GetDirectoryName(fullPath), null);
        }

        public static ConfigSource FromUrl(string url)
        {
            var uri = new Uri(url);
            return new ConfigSource(uri.ToString(), true, null, uri);
        }
    }
}
