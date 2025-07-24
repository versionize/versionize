using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.Git;
using static Versionize.CommandLine.CommandLineUI;

namespace Versionize.Lifecycle;

public sealed class ChangeCommitter
{
    public static void CreateCommit(
        Repository repo,
        Options options,
        SemanticVersion nextVersion,
        IBumpFile bumpFile,
        ChangelogBuilder? changelog)
    {
        if (options.SkipCommit || options.DryRun)
        {
            return;
        }

        if (changelog is not null)
        {
            Commands.Stage(repo, changelog.FilePath);
        }

        var projectFiles = bumpFile.GetFilePaths();
        if (projectFiles.Any())
        {
            Commands.Stage(repo, projectFiles);
        }

        var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = false });
        if (!status.Staged.Any() && !status.Added.Any())
        {
            return;
        }

        var author = repo.Config.BuildSignature(DateTimeOffset.Now);
        var committer = author;
        var releaseCommitMessage = $"chore(release): {nextVersion} {options.CommitSuffix}".TrimEnd();

        if (options.Sign)
        {
            GitProcessUtil.CreateSignedCommit(options.WorkingDirectory, releaseCommitMessage);
        }
        else
        {
            repo.Commit(releaseCommitMessage, author, committer);
        }

        // TODO: Make this message dynamic
        Step("committed changes in projects and CHANGELOG.md");
    }

    public sealed class Options
    {
        public bool SkipCommit { get; init; }
        public bool DryRun { get; init; }
        public bool Sign { get; init; }
        public string? CommitSuffix { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                CommitSuffix = versionizeOptions.CommitSuffix,
                DryRun = versionizeOptions.DryRun,
                Sign = versionizeOptions.Sign,
                SkipCommit = versionizeOptions.SkipCommit,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}
