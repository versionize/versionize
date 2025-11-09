using McMaster.Extensions.CommandLineUtils;
using Versionize.BumpFiles;
using Versionize.CommandLine;

namespace Versionize.Config;

public static class ConfigProvider
{
    public static VersionizeOptions GetSelectedOptions(
        string cwd,
        CliConfig cliConfig,
        FileConfig? fileConfig)
    {
        var options = MergeWithOptions(
            cwd,
            fileConfig,
            cliConfig);

        ValidateChangelogPaths(fileConfig, options.WorkingDirectory);

        CommandLineUI.Verbosity = MergeBool(cliConfig.Silent, fileConfig?.Silent)
            ? LogLevel.Silent
            : LogLevel.All;

        return options;
    }

    private static VersionizeOptions MergeWithOptions(
        string baseWorkingDirectory,
        FileConfig? fileConfig,
        CliConfig cliConfig)
    {
        string? projectName = cliConfig.ProjectName.Value();
        var project =
            fileConfig?.Projects.FirstOrDefault(x =>
                x.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        if (project != null)
        {
            project.Changelog =
                ChangelogOptions.Merge(project.Changelog,
                    ChangelogOptions.Merge(fileConfig?.Changelog, ChangelogOptions.Default));
        }
        else
        {
            project = ProjectOptions.DefaultOneProjectPerRepo;
            project.Changelog =
                ChangelogOptions.Merge(fileConfig?.Changelog, ChangelogOptions.Default);
        }

        var commitParser = CommitParserOptions.Merge(fileConfig?.CommitParser, CommitParserOptions.Default);
        var tagOnly = MergeBool(cliConfig.TagOnly, fileConfig?.TagOnly);
        var projectPath = Path.Combine(baseWorkingDirectory, project.Path);
        var bumpFileType = BumpFileTypeDetector.GetType(projectPath, tagOnly);

        return new VersionizeOptions
        {
            WorkingDirectory = projectPath,
            DryRun = MergeBool(cliConfig.DryRun, fileConfig?.DryRun),
            ReleaseAs = cliConfig.ReleaseAs.Value() ?? fileConfig?.ReleaseAs,
            SkipDirty = MergeBool(cliConfig.SkipDirty, fileConfig?.SkipDirty),
            SkipCommit = MergeBool(cliConfig.SkipCommit, fileConfig?.SkipCommit),
            SkipTag = MergeBool(cliConfig.SkipTag, fileConfig?.SkipTag),
            SkipChangelog = MergeBool(cliConfig.SkipChangelog, fileConfig?.SkipChangelog),
            IgnoreInsignificantCommits = MergeBool(cliConfig.IgnoreInsignificant, fileConfig?.IgnoreInsignificantCommits),
            ExitInsignificantCommits = MergeBool(cliConfig.ExitInsignificant, fileConfig?.ExitInsignificantCommits),
            CommitSuffix = cliConfig.CommitSuffix.Value() ?? fileConfig?.CommitSuffix,
            Prerelease = cliConfig.Prerelease.Value() ?? fileConfig?.Prerelease,
            AggregatePrereleases = MergeBool(cliConfig.AggregatePrereleases, fileConfig?.AggregatePrereleases),
            FirstParentOnlyCommits = MergeBool(cliConfig.FirstParentOnlyCommits, fileConfig?.FirstParentOnlyCommits),
            Sign = MergeBool(cliConfig.Sign, fileConfig?.Sign),
            BumpFileType = bumpFileType,
            CommitParser = commitParser,
            Project = project,
            UseCommitMessageInsteadOfTagToFindLastReleaseCommit = MergeBool(cliConfig.UseCommitMessageInsteadOfTagToFindLastReleaseCommit, false),
        };
    }

    private static bool MergeBool(CommandOption cliOption, bool? fileValue)
    {
        // If CLI option has a value, parse it
        if (cliOption.HasValue())
        {
            var optionValue = cliOption.Value();
            
            // If no explicit value provided (flag without value), treat as true
            if (string.IsNullOrEmpty(optionValue))
            {
                return true;
            }
            
            // Try to parse the explicit value
            if (bool.TryParse(optionValue, out var boolValue))
            {
                return boolValue;
            }
            
            // If parsing fails, treat flag presence as true
            return true;
        }
        
        // Fall back to file config or default false
        return fileValue ?? false;
    }

    private static void ValidateChangelogPaths(FileConfig? fileConfig, string cwd)
    {
        if (fileConfig?.Projects is null)
        {
            return;
        }

        var changelogPaths = new HashSet<string>();

        foreach (var project in fileConfig.Projects)
        {
            var changelogPath = Path.Combine(cwd, project.Path, project.Changelog?.Path ?? string.Empty);
            var fullChangelogPath = Path.GetFullPath(changelogPath);

            if (!changelogPaths.Add(fullChangelogPath))
            {
                CommandLineUI.Exit("Two or more projects have changelog paths pointing to the same location.", 1);
            }
        }
    }
}
