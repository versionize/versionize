using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Git;

using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.Pipeline.VersionizeSteps;

public class CreateTagStep : IPipelineStep<CreateCommitResult, CreateTagStep.Options, CreateTagResult>
{
    public CreateTagResult Execute(CreateCommitResult input, Options options)
    {
        Repository repo = input.Repository;
        SemanticVersion nextVersion = input.BumpedVersion;
        CreateTag(repo, options, nextVersion);
        return new CreateTagResult
        {
            Repository = input.Repository,
            BumpFile = input.BumpFile,
            Version = input.Version,
            IsFirstRelease = input.IsFirstRelease,
            Commits = input.Commits,
            BumpedVersion = input.BumpedVersion,
            ChangelogPath = input.ChangelogPath,
            Commit = input.Commit,
            Tag = repo.Tags[options.Project.GetTagName(nextVersion)],
        };
    }

    private static void CreateTag(
        Repository repo,
        Options options,
        SemanticVersion nextVersion)
    {
        if (options.SkipTag || options.DryRun)
        {
            return;
        }

        if (repo.VersionTagsExists(nextVersion, options.Project))
        {
            throw new VersionizeException(ErrorMessages.VersionAlreadyExists(nextVersion.ToNormalizedString()), 1);
        }

        var tagName = options.Project.GetTagName(nextVersion);
        if (options.Sign)
        {
            GitProcessUtil.CreateSignedTag(options.WorkingDirectory, tagName, $"{nextVersion}");
        }
        else
        {
            var tagger = repo.Config.BuildSignature(DateTimeOffset.Now);
            repo.ApplyTag(tagName, tagger, $"{nextVersion}");
        }

        Step(InfoMessages.TaggedRelease(tagName, repo.Head.Tip.Sha));
    }

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public bool SkipTag { get; init; }
        public bool DryRun { get; init; }
        public bool Sign { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                Sign = versionizeOptions.Sign,
                SkipTag = versionizeOptions.SkipTag,
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
