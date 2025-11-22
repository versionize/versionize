using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.ConventionalCommits;
using static Versionize.CommandLine.CommandLineUI;
using Versionize.CommandLine;

namespace Versionize.Lifecycle;

public sealed class ChangelogUpdater
{
    public static ChangelogBuilder? Update(
        Repository repo,
        Options options,
        SemanticVersion nextVersion,
        SemanticVersion? previousVersion,
        IReadOnlyList<ConventionalCommit> conventionalCommits)
    {
        if (options.SkipChangelog)
        {
            return null;
        }

        var versionTime = DateTimeOffset.Now;
        var changelog = ChangelogBuilder.CreateForPath(Path.GetFullPath(Path.Combine(options.WorkingDirectory, options.Project.Changelog.Path ?? "")));
        var changelogLinkBuilder = LinkBuilderFactory.CreateFor(repo, options.Project.Changelog.LinkTemplates);
        previousVersion ??= nextVersion;

        if (options.DryRun)
        {
            string markdown = ChangelogBuilder.GenerateMarkdown(
                nextVersion,
                previousVersion,
                versionTime,
                changelogLinkBuilder,
                conventionalCommits,
                options.Project,
                options.Aliases);
            DryRun(markdown.TrimEnd('\n'));
        }
        else
        {
            changelog.Write(
                nextVersion,
                previousVersion,
                versionTime,
                changelogLinkBuilder,
                conventionalCommits,
                options.Project,
                options.Aliases);
        }
        Step(InfoMessages.UpdatedChangelog());

        return changelog;
    }

    public sealed class Options
    {
        public bool SkipChangelog { get; init; }
        public bool DryRun { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }
        public IReadOnlyDictionary<string, string[]>? Aliases { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipChangelog = versionizeOptions.SkipChangelog,
                Project = versionizeOptions.Project,
                WorkingDirectory = versionizeOptions.WorkingDirectory ??
                    throw new VersionizeException(nameof(versionizeOptions.WorkingDirectory), 1),
                Aliases = versionizeOptions.Aliases,
            };
        }
    }
}
