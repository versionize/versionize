﻿using Versionize.BumpFiles;
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

        CommandLineUI.Verbosity = MergeBool(cliConfig.Silent.HasValue(), fileConfig?.Silent)
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
        var tagOnly = MergeBool(cliConfig.TagOnly.HasValue(), fileConfig?.TagOnly);
        var projectPath = Path.Combine(baseWorkingDirectory, project.Path);
        var bumpFileType = BumpFileTypeDetector.GetType(projectPath, tagOnly);

        return new VersionizeOptions
        {
            WorkingDirectory = projectPath,
            DryRun = MergeBool(cliConfig.DryRun.HasValue(), fileConfig?.DryRun),
            ReleaseAs = cliConfig.ReleaseAs.Value() ?? fileConfig?.ReleaseAs,
            SkipDirty = MergeBool(cliConfig.SkipDirty.HasValue(), fileConfig?.SkipDirty),
            SkipCommit = MergeBool(cliConfig.SkipCommit.HasValue(), fileConfig?.SkipCommit),
            SkipTag = MergeBool(cliConfig.SkipTag.HasValue(), fileConfig?.SkipTag),
            SkipChangelog = MergeBool(cliConfig.SkipChangelog.HasValue(), fileConfig?.SkipChangelog),
            IgnoreInsignificantCommits = MergeBool(cliConfig.IgnoreInsignificant.HasValue(), fileConfig?.IgnoreInsignificantCommits),
            ExitInsignificantCommits = MergeBool(cliConfig.ExitInsignificant.HasValue(), fileConfig?.ExitInsignificantCommits),
            CommitSuffix = cliConfig.CommitSuffix.Value() ?? fileConfig?.CommitSuffix,
            Prerelease = cliConfig.Prerelease.Value() ?? fileConfig?.Prerelease,
            AggregatePrereleases = MergeBool(cliConfig.AggregatePrereleases.HasValue(), fileConfig?.AggregatePrereleases),
            FirstParentOnlyCommits = MergeBool(cliConfig.FirstParentOnlyCommits.HasValue(), fileConfig?.FirstParentOnlyCommits),
            Sign = MergeBool(cliConfig.Sign.HasValue(), fileConfig?.Sign),
            BumpFileType = bumpFileType,
            CommitParser = commitParser,
            Project = project,
            UseCommitMessageInsteadOfTagToFindLastReleaseCommit = cliConfig.UseCommitMessageInsteadOfTagToFindLastReleaseCommit.HasValue(),
        };
    }

    private static bool MergeBool(bool overridingValue, bool? optionalValue)
    {
        return overridingValue ? overridingValue : (optionalValue ?? false);
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
