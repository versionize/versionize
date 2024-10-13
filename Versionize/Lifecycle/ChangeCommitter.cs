﻿using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
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
        repo.Commit(releaseCommitMessage, author, committer);
        Step("committed changes in projects and CHANGELOG.md");
    }
    
    public sealed class Options
    {
        public bool SkipCommit { get; init; }
        public bool DryRun { get; init; }
        public string CommitSuffix { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                CommitSuffix = versionizeOptions.CommitSuffix,
                DryRun = versionizeOptions.DryRun,
                SkipCommit = versionizeOptions.SkipCommit,
            };
        }
    }
}
