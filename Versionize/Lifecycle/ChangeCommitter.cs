using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.Git;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize;

public sealed class ChangeCommitter
{
    public static void CreateCommit(
        Repository repo,
        Options options,
        SemanticVersion nextVersion,
        IBumpFile bumpFile,
        ChangelogBuilder changelog)
    {
        if (options.SkipCommit || options.DryRun)
        {
            return;
        }
        if (options.TagOnly)
        {
            return;
        }

        // TODO: Validate in Tagger too, or do a single check at the very beginning.
        if (!repo.IsConfiguredForCommits())
        {
            Exit(@"Warning: Git configuration is missing. Please configure git before running versionize:
git config --global user.name ""John Doe""
$ git config --global user.email johndoe@example.com", 1);
        }

        Commands.Stage(repo, changelog.FilePath);
        Commands.Stage(repo, bumpFile.GetFilePaths());
        var author = repo.Config.BuildSignature(DateTimeOffset.Now);
        var committer = author;
        var releaseCommitMessage = $"chore(release): {nextVersion} {options.CommitSuffix}".TrimEnd();
        repo.Commit(releaseCommitMessage, author, committer);
        Step("committed changes in projects and CHANGELOG.md");
    }
    
    public sealed class Options
    {
        public bool SkipCommit { get; init; }
        public bool DryRun { get; init; }
        public bool TagOnly { get; init; }
        public string CommitSuffix { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                CommitSuffix = versionizeOptions.CommitSuffix,
                DryRun = versionizeOptions.DryRun,
                SkipCommit = versionizeOptions.SkipCommit,
                TagOnly = versionizeOptions.TagOnly,
            };
        }
    }
}
