using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.ConventionalCommits;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public sealed class ChangelogUpdater
{
    public static ChangelogBuilder Update(
        Repository repo,
        Options options,
        SemanticVersion nextVersion,
        IReadOnlyList<ConventionalCommit> conventionalCommits)
    {
        if (options.SkipChangelog)
        {
            return null;
        }

        var versionTime = DateTimeOffset.Now;
        var changelog = ChangelogBuilder.CreateForPath(Path.GetFullPath(Path.Combine(options.WorkingDirectory, options.Project.Changelog.Path)));
        var changelogLinkBuilder = LinkBuilderFactory.CreateFor(repo, options.Project.Changelog.LinkTemplates);

        if (options.DryRun)
        {
            string markdown = ChangelogBuilder.GenerateMarkdown(
                nextVersion,
                versionTime,
                changelogLinkBuilder,
                conventionalCommits,
                options.Project.Changelog);
            DryRun(markdown.TrimEnd('\n'));
        }
        else
        {
            changelog.Write(
                nextVersion,
                versionTime,
                changelogLinkBuilder,
                conventionalCommits,
                options.Project.Changelog);
        }
        Step("updated CHANGELOG.md");

        return changelog;
    }

    public sealed class Options
    {
        public bool SkipChangelog { get; init; }
        public bool DryRun { get; init; }
        public ProjectOptions Project { get; init; }
        public string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipChangelog = versionizeOptions.SkipChangelog,
                Project = versionizeOptions.Project,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}
