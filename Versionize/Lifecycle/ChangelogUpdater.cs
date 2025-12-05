using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.ConventionalCommits;
using static Versionize.CommandLine.CommandLineUI;
using Versionize.CommandLine;
using Versionize.Changelog.LinkBuilders;

using Input = Versionize.Lifecycle.IChangelogUpdater.Input;
using Options = Versionize.Lifecycle.IChangelogUpdater.Options;

namespace Versionize.Lifecycle;

public sealed class ChangelogUpdater : IChangelogUpdater
{
    public ChangelogBuilder? Update(Input input, Options options)
    {
        var repo = input.Repository;
        var nextVersion = input.NewVersion;
        var previousVersion = input.OriginalVersion;
        var conventionalCommits = input.ConventionalCommits;

        if (options.SkipChangelog)
        {
            return null;
        }

        var versionTime = DateTimeOffset.Now;
        var changelogPath = Path.GetFullPath(Path.Combine(options.WorkingDirectory, options.Project.Changelog.Path ?? ""));
        var changelog = ChangelogBuilder.CreateForPath(changelogPath);
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
                options.Project);
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
                options.Project);
        }
        Step(InfoMessages.UpdatedChangelog());

        return changelog;
    }
}

public interface IChangelogUpdater
{
    ChangelogBuilder? Update(Input input, Options options);

    sealed class Input
    {
        public required Repository Repository { get; init; }
        public required SemanticVersion NewVersion { get; init; }
        public required SemanticVersion? OriginalVersion { get; init; }
        public required IReadOnlyList<ConventionalCommit> ConventionalCommits { get; init; }
    }

    sealed class Options
    {
        public bool SkipChangelog { get; init; }
        public bool DryRun { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

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
