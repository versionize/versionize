using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.Git;
using Versionize.CommandLine;

using static Versionize.CommandLine.CommandLineUI;
using Input = Versionize.Lifecycle.IReleaseTagger.Input;
using Options = Versionize.Lifecycle.IReleaseTagger.Options;

namespace Versionize.Lifecycle;

public sealed class ReleaseTagger(IGitIdentityResolver gitIdentityResolver) : IReleaseTagger
{
    private readonly IGitIdentityResolver _gitIdentityResolver = gitIdentityResolver;

    public void CreateTag(Input input, Options options)
    {
        var repo = input.Repository;
        var nextVersion = input.NewVersion;

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
            var gitConfigArguments = _gitIdentityResolver.BuildGitConfigArguments(repo);
            GitProcessUtil.CreateSignedTag(options.WorkingDirectory, tagName, $"{nextVersion}", gitConfigArguments);
        }
        else
        {
            var tagger = _gitIdentityResolver.BuildSignature(repo, DateTimeOffset.Now);
            repo.ApplyTag(tagName, tagger, $"{nextVersion}");
        }

        Step(InfoMessages.TaggedRelease(tagName, repo.Head.Tip.Sha));
    }
}

public interface IReleaseTagger
{
    void CreateTag(Input input, Options options);

    sealed class Input
    {
        public required Repository Repository { get; init; }
        public required SemanticVersion NewVersion { get; init; }
    }

    sealed class Options
    {
        public bool SkipTag { get; init; }
        public bool DryRun { get; init; }
        public bool Sign { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
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
    }

}
