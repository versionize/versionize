using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Config;
using Versionize.Git;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

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
        var tagger = repo.Config.BuildSignature(DateTimeOffset.Now);
        repo.ApplyTag(tagName, tagger, $"{nextVersion}");
        Step($"tagged release as {tagName} against commit with sha {repo.Head.Tip.Sha}");
    }
    
    public sealed class Options
    {
        public bool SkipTag { get; init; }
        public bool DryRun { get; init; }
        public ProjectOptions Project { get; init; }

        public static implicit operator ReleaseTagger.Options(VersionizeOptions versionizeOptions)
        {
            return new ReleaseTagger.Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipTag = versionizeOptions.SkipTag,
                Project = versionizeOptions.Project,
            };
        }
    }
}
