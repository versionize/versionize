using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Changelog.LinkBuilders;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.ConventionalCommits;

using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.Pipeline.VersionizeSteps;

public class UpdateChangelogStep : IPipelineStep<BumpVersionResult, UpdateChangelogStep.Options, UpdateChangelogResult>
{
    public UpdateChangelogResult Execute(BumpVersionResult input, Options options)
    {
        var changelog = Update(
            input.Repository,
            options,
            input.BumpedVersion,
            input.Version,
            input.Commits);

        return new UpdateChangelogResult
        {
            Repository = input.Repository,
            BumpFile = input.BumpFile,
            Version = input.Version,
            IsFirstRelease = input.IsFirstRelease,
            Commits = input.Commits,
            BumpedVersion = input.BumpedVersion,
            ChangelogPath = changelog?.FilePath,
        };
    }

    private static Changelog.Changelog? Update(
        Repository repo,
        Options options,
        SemanticVersion nextVersion,
        SemanticVersion? previousVersion,
        IReadOnlyList<ConventionalCommit> conventionalCommits)
    {
        // TODO: Consider returning an instance of a NoOpChangelogBuilder instead of null
        if (options.SkipChangelog)
        {
            return null;
        }

        var versionTime = DateTimeOffset.Now;
        var changelog = Changelog.Changelog.CreateForPath(Path.GetFullPath(Path.Combine(options.WorkingDirectory, options.Project.Changelog.Path ?? "")));
        var changelogLinkBuilder = LinkBuilderFactory.CreateFor(repo, options.Project.Changelog.LinkTemplates);
        previousVersion ??= nextVersion;

        if (options.DryRun)
        {
            string markdown = Changelog.Changelog.GenerateMarkdown(
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

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public bool SkipChangelog { get; init; }
        public bool DryRun { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipChangelog = versionizeOptions.SkipChangelog,
                Project = versionizeOptions.Project,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
