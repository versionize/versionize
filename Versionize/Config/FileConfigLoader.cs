using Versionize.CommandLine;

namespace Versionize.Config;

public static class FileConfigLoader
{
    private const int MaxDepth = 5;
    private static readonly HttpClient DefaultHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public static Func<HttpClient> HttpClientFactory { get; set; } = () => DefaultHttpClient;

    public static FileConfig? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var source = ConfigSource.FromFile(filePath);

        return LoadInternal(source, visited, 0);
    }

    private static FileConfig? LoadInternal(ConfigSource source, HashSet<string> visited, int depth)
    {
        if (depth > MaxDepth)
        {
            CommandLineUI.Exit($"Exceeded maximum config inheritance depth ({MaxDepth}).", 1);
            return null;
        }

        if (!visited.Add(source.Key))
        {
            CommandLineUI.Exit($"Detected circular config inheritance for {source.Display}.", 1);
            return null;
        }

        var current = LoadSource(source);
        if (current == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(current.Extends))
        {
            return current;
        }

        var parentSource = ResolveExtends(source, current.Extends);
        var parent = LoadInternal(parentSource, visited, depth + 1);

        return FileConfigMerger.Merge(parent, current);
    }

    private static FileConfig? LoadSource(ConfigSource source)
    {
        if (source.IsUrl)
        {
            try
            {
                var jsonString = HttpClientFactory().GetStringAsync(source.Uri!).GetAwaiter().GetResult();
                return FileConfig.Deserialize(jsonString, source.Display);
            }
            catch (Exception e)
            {
                CommandLineUI.Exit($"Failed to fetch config from {source.Display}: {e.Message}", 1);
                return null;
            }
        }

        if (!File.Exists(source.FilePath))
        {
            CommandLineUI.Exit($"Config file not found: {source.FilePath}", 1);
            return null;
        }

        var fileContents = File.ReadAllText(source.FilePath!);
        return FileConfig.Deserialize(fileContents, source.Display);
    }

    private static ConfigSource ResolveExtends(ConfigSource current, string extendsValue)
    {
        if (TryCreateHttpUri(extendsValue, out var absoluteUri))
        {
            return ConfigSource.FromUrl(absoluteUri);
        }

        if (current.IsUrl && Uri.TryCreate(current.Uri, extendsValue, out var relativeUri) && IsHttpScheme(relativeUri))
        {
            return ConfigSource.FromUrl(relativeUri);
        }

        if (Path.IsPathRooted(extendsValue))
        {
            return ConfigSource.FromFile(extendsValue);
        }

        if (current.IsUrl)
        {
            CommandLineUI.Exit("Relative config paths are not supported for URL-based configs.", 1);
            return current;
        }

        var baseDirectory = current.Directory;
        var resolvedPath = Path.Combine(baseDirectory, extendsValue);

        return ConfigSource.FromFile(resolvedPath);
    }

    private static bool TryCreateHttpUri(string value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var candidate) && IsHttpScheme(candidate))
        {
            uri = candidate;
            return true;
        }

        uri = null!;
        return false;
    }

    private static bool IsHttpScheme(Uri uri)
    {
        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ConfigSource
    {
        private ConfigSource(string display, string? filePath, Uri? uri)
        {
            Display = display;
            FilePath = filePath;
            Uri = uri;
        }

        public string Display { get; }
        public string? FilePath { get; }
        public Uri? Uri { get; }
        public bool IsUrl => Uri != null;
        public string Key => IsUrl ? Uri!.AbsoluteUri : Path.GetFullPath(FilePath!);
        public string Directory => Path.GetDirectoryName(FilePath!) ?? string.Empty;

        public static ConfigSource FromFile(string filePath)
        {
            var fullPath = Path.GetFullPath(filePath);
            return new ConfigSource(fullPath, fullPath, null);
        }

        public static ConfigSource FromUrl(Uri uri)
        {
            return new ConfigSource(uri.AbsoluteUri, null, uri);
        }
    }

    private static class FileConfigMerger
    {
        public static FileConfig? Merge(FileConfig? baseConfig, FileConfig? overrideConfig)
        {
            if (baseConfig == null)
            {
                return overrideConfig;
            }

            if (overrideConfig == null)
            {
                return baseConfig;
            }

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
                CommitParser = MergeCommitParser(baseConfig.CommitParser, overrideConfig.CommitParser),
                Changelog = MergeChangelog(baseConfig.Changelog, overrideConfig.Changelog),
                Projects = MergeProjects(baseConfig.Projects, overrideConfig.Projects)
            };
        }

        private static CommitParserOptions? MergeCommitParser(CommitParserOptions? baseOptions, CommitParserOptions? overrideOptions)
        {
            if (baseOptions == null && overrideOptions == null)
            {
                return null;
            }

            var commitParserOptions = new CommitParserOptions();
            if (baseOptions != null)
            {
                commitParserOptions.HeaderPatterns = baseOptions.HeaderPatterns;
                commitParserOptions.IssuesPatterns = baseOptions.IssuesPatterns;
            }

            if (overrideOptions != null)
            {
                commitParserOptions.HeaderPatterns ??= overrideOptions.HeaderPatterns;
                commitParserOptions.IssuesPatterns ??= overrideOptions.IssuesPatterns;
            }

            return commitParserOptions;
        }

        private static ChangelogOptions? MergeChangelog(ChangelogOptions? baseOptions, ChangelogOptions? overrideOptions)
        {
            if (baseOptions == null && overrideOptions == null)
            {
                return null;
            }

            return new ChangelogOptions
            {
                Header = overrideOptions?.Header ?? baseOptions?.Header,
                Path = overrideOptions?.Path ?? baseOptions?.Path,
                IncludeAllCommits = overrideOptions?.IncludeAllCommits ?? baseOptions?.IncludeAllCommits,
                Sections = overrideOptions?.Sections ?? baseOptions?.Sections,
                LinkTemplates = MergeLinkTemplates(baseOptions?.LinkTemplates, overrideOptions?.LinkTemplates)
            };
        }

        private static ChangelogLinkTemplates? MergeLinkTemplates(ChangelogLinkTemplates? baseTemplates, ChangelogLinkTemplates? overrideTemplates)
        {
            if (baseTemplates == null && overrideTemplates == null)
            {
                return null;
            }

            if (baseTemplates == null)
            {
                return overrideTemplates;
            }

            if (overrideTemplates == null)
            {
                return baseTemplates;
            }

            return new ChangelogLinkTemplates
            {
                IssueLink = overrideTemplates.IssueLink ?? baseTemplates.IssueLink,
                CommitLink = overrideTemplates.CommitLink ?? baseTemplates.CommitLink,
                VersionTagLink = overrideTemplates.VersionTagLink ?? baseTemplates.VersionTagLink
            };
        }

        private static ProjectOptions[] MergeProjects(ProjectOptions[]? baseProjects, ProjectOptions[]? overrideProjects)
        {
            var merged = new List<ProjectOptions>();
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var baseList = baseProjects ?? [];
            var overrideList = overrideProjects ?? [];

            for (var index = 0; index < baseList.Length; index++)
            {
                var project = baseList[index];
                merged.Add(project);
                map[project.Name] = index;
            }

            foreach (var project in overrideList)
            {
                if (map.TryGetValue(project.Name, out var existingIndex))
                {
                    merged[existingIndex] = project;
                }
                else
                {
                    map[project.Name] = merged.Count;
                    merged.Add(project);
                }
            }

            return [.. merged];
        }
    }
}
