using McMaster.Extensions.CommandLineUtils;
using Versionize.CommandLine;

namespace Versionize.Config;

public static class ConfigProvider
{
    public static VersionizeOptions GetSelectedOptions(
        string cwd,
        CliConfig cliConfig,
        FileConfig? fileConfig)
    {
        ProjectOptions? project = GetProjectOptions(fileConfig, cliConfig);
        CommitParserOptions commitParser = CommitParserOptions.MergeWithDefault(fileConfig?.CommitParser);

        var projectPath = Path.Combine(cwd, project.Path);

        ValidateChangelogPaths(fileConfig, projectPath);
        ValidateVersionElement(project.VersionElement);

        return new VersionizeOptions
        {
            Silent = MergeBool(cliConfig.Silent, fileConfig?.Silent),
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
            SkipBumpFile = MergeBool(cliConfig.TagOnly, fileConfig?.TagOnly),
            CommitParser = commitParser,
            Project = project,
            FindReleaseCommitViaMessage = MergeBool(cliConfig.FindReleaseCommitViaMessage, false),
        };
    }

    private static ProjectOptions GetProjectOptions(FileConfig? fileConfig, CliConfig cliConfig)
    {
        string? projectName = cliConfig.ProjectName.Value();
        var project =
            fileConfig?.Projects.FirstOrDefault(x =>
                x.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        if (project != null)
        {
            project = project with
            {
                Changelog = ChangelogOptions.Merge(project.Changelog,
                    ChangelogOptions.Merge(fileConfig?.Changelog, ChangelogOptions.Default))
            };
        }
        else
        {
            project = ProjectOptions.DefaultOneProjectPerRepo;
            if (fileConfig?.Changelog != null)
            {
                project = project with
                {
                    Changelog = ChangelogOptions.Merge(fileConfig?.Changelog, ChangelogOptions.Default)
                };
            }

            var tagTemplate = cliConfig.TagTemplate.Value() ?? fileConfig?.TagTemplate;
            if (tagTemplate != null)
            {
                project = project with { TagTemplate = tagTemplate };
            }
        }

        return project;
    }

    private static bool MergeBool(CommandOption<bool> cliOption, bool? fileValue)
    {
        if (cliOption.HasValue())
        {
            return cliOption.ParsedValue;
        }

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
                throw new VersionizeException(ErrorMessages.DuplicateChangelogPaths(), 1);
            }
        }
    }

    private static void ValidateVersionElement(string? versionElement)
    {
        if (string.IsNullOrEmpty(versionElement))
        {
            return;
        }

        foreach (var ch in versionElement)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_'))
            {
                throw new VersionizeException(ErrorMessages.InvalidVersionElement(versionElement), 1);
            }
        }
    }
}
