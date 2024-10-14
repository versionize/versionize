using System.Text.Json;
using Versionize.CommandLine;

namespace Versionize.Config;

public static class ConfigProvider
{
    public static VersionizeOptions GetSelectedOptions(string cwd, CliConfig cliConfig)
    {
        var configDirectory = cliConfig.ConfigurationDirectory.Value() ?? cwd;
        var fileConfigPath = Path.Join(configDirectory, ".versionize");
        var fileConfig = FromJsonFile(fileConfigPath);

        var options = MergeWithOptions(
            cwd,
            fileConfig,
            cliConfig);

        CommandLineUI.Verbosity = MergeBool(cliConfig.Silent.HasValue(), fileConfig?.Silent)
            ? LogLevel.Silent
            : LogLevel.All;

        return options;
    }

    private static FileConfig FromJsonFile(string filePath)
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
            CommandLineUI.Exit($"Failed to parse .versionize file: {e.Message}", 1);
            return null;
        }
    }

    private static VersionizeOptions MergeWithOptions(
        string baseWorkingDirectory,
        FileConfig fileConfig,
        CliConfig cliConfig)
    {
        string projectName = cliConfig.ProjectName.Value();
        var project =
            fileConfig?.Projects.FirstOrDefault(x =>
                x.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        if (project != null)
        {
            project.Changelog =
                MergeChangelogOptions(project.Changelog,
                    MergeChangelogOptions(fileConfig?.Changelog, ChangelogOptions.Default));
        }
        else
        {
            project = ProjectOptions.DefaultOneProjectPerRepo;
            project.Changelog =
                MergeChangelogOptions(fileConfig?.Changelog, ChangelogOptions.Default);
        }

        var commit = MergeCommitOptions(fileConfig?.CommitParser, CommitParserOptions.Default);

        return new VersionizeOptions
        {
            WorkingDirectory = Path.Combine(baseWorkingDirectory, project.Path),
            DryRun = MergeBool(cliConfig.DryRun.HasValue(), fileConfig?.DryRun),
            ReleaseAs = cliConfig.ReleaseAs.Value() ?? fileConfig?.ReleaseAs,
            SkipDirty = MergeBool(cliConfig.SkipDirty.HasValue(), fileConfig?.SkipDirty),
            SkipCommit = MergeBool(cliConfig.SkipCommit.HasValue(), fileConfig?.SkipCommit),
            SkipTag = MergeBool(cliConfig.SkipTag.HasValue(), fileConfig?.SkipTag),
            SkipChangelog = MergeBool(cliConfig.SkipChangelog.HasValue(), fileConfig?.SkipChangelog),
            TagOnly = MergeBool(cliConfig.TagOnly.HasValue(), fileConfig?.TagOnly),
            IgnoreInsignificantCommits = MergeBool(cliConfig.IgnoreInsignificant.HasValue(), fileConfig?.IgnoreInsignificantCommits),
            ExitInsignificantCommits = MergeBool(cliConfig.ExitInsignificant.HasValue(), fileConfig?.ExitInsignificantCommits),
            CommitSuffix = cliConfig.CommitSuffix.Value() ?? fileConfig?.CommitSuffix,
            Prerelease = cliConfig.Prerelease.Value() ?? fileConfig?.Prerelease,
            AggregatePrereleases = MergeBool(cliConfig.AggregatePrereleases.HasValue(), fileConfig?.AggregatePrereleases),
            CommitParser = commit,
            Project = project,
            UseCommitMessageInsteadOfTagToFindLastReleaseCommit = cliConfig.UseCommitMessageInsteadOfTagToFindLastReleaseCommit.HasValue(),
        };
    }

    private static CommitParserOptions MergeCommitOptions(CommitParserOptions customOptions, CommitParserOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new CommitParserOptions
        {
            HeaderPatterns = customOptions.HeaderPatterns ?? defaultOptions.HeaderPatterns
        };
    }

    private static ChangelogOptions MergeChangelogOptions(ChangelogOptions customOptions, ChangelogOptions defaultOptions)
    {
        if (customOptions == null)
        {
            return defaultOptions;
        }

        return new ChangelogOptions
        {
            Header = customOptions.Header ?? defaultOptions.Header,
            Sections = customOptions.Sections ?? defaultOptions.Sections,
            LinkTemplates = customOptions.LinkTemplates ?? defaultOptions.LinkTemplates,
            Path = customOptions.Path ?? defaultOptions.Path,
        };
    }

    private static bool MergeBool(bool overridingValue, bool? optionalValue)
    {
        return overridingValue ? overridingValue : (optionalValue ?? false);
    }
}
