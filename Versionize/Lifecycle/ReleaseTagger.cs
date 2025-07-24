using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.Git;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.Lifecycle;

public sealed class ReleaseTagger
{
    public static void CreateTag(
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
            Exit($"Version {nextVersion} already exists. Please use a different version.", 1);
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

        Step($"tagged release as {tagName} against commit with sha {repo.Head.Tip.Sha}");
    }

    public sealed class Options
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
