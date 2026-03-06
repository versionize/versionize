using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.Git;
using Versionize.CommandLine;

using static Versionize.CommandLine.CommandLineUI;
using Input = Versionize.Lifecycle.IReleaseCommitter.Input;
using Options = Versionize.Lifecycle.IReleaseCommitter.Options;

namespace Versionize.Lifecycle;

public sealed class ReleaseCommitter(IGitIdentityResolver gitIdentityResolver) : IReleaseCommitter
{
    private readonly IGitIdentityResolver _gitIdentityResolver = gitIdentityResolver;

    public void CreateCommit(Input input, Options options)
    {
        var repo = input.Repository;
        var nextVersion = input.NewVersion;
        var bumpFile = input.BumpFile;
        var changelog = input.Changelog;

        if (options.SkipCommit || options.DryRun)
        {
            return;
        }

        if (changelog is not null)
        {
            LibGit2Sharp.Commands.Stage(repo, changelog.FilePath);
        }

        var projectFiles = bumpFile?.GetFilePaths() ?? [];
        if (projectFiles.Any())
        {
            LibGit2Sharp.Commands.Stage(repo, projectFiles);
        }

        var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = false });
        if (!status.Staged.Any() && !status.Added.Any())
        {
            return;
        }

        var identity = _gitIdentityResolver.Resolve(repo);
        var author = BuildSignature(identity, DateTimeOffset.Now);
        var committer = author;
        var releaseCommitMessage = $"chore(release): {nextVersion} {options.CommitSuffix}".TrimEnd();

        if (options.Sign)
        {
            var gitConfigArguments = BuildGitConfigArguments(identity);
            GitProcessUtil.CreateSignedCommit(options.WorkingDirectory, releaseCommitMessage, gitConfigArguments);
        }
        else
        {
            repo.Commit(releaseCommitMessage, author, committer);
        }

        // TODO: Make this message dynamic
        Step(InfoMessages.CommittedChanges(changelog?.FilePath ?? "CHANGELOG.md"));
    }

    private static Signature BuildSignature(GitIdentity identity, DateTimeOffset now)
    {
        if (!identity.IsConfigured)
        {
            throw new VersionizeException(ErrorMessages.GitConfigMissing(), 1);
        }

        return new Signature(identity.UserName, identity.UserEmail, now);
    }

    private static string BuildGitConfigArguments(GitIdentity identity)
    {
        if (!identity.IsConfigured)
        {
            return string.Empty;
        }

        return $"-c user.name=\"{EscapeGitConfigValue(identity.UserName!)}\" -c user.email=\"{EscapeGitConfigValue(identity.UserEmail!)}\"";
    }

    private static string EscapeGitConfigValue(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}

public interface IReleaseCommitter
{
    void CreateCommit(Input input, Options options);

    class Input
    {
        public required IRepository Repository { get; init; }
        public required SemanticVersion NewVersion { get; init; }
        public required IBumpFile? BumpFile { get; init; }
        public required ChangelogBuilder? Changelog { get; init; }
    }

    sealed class Options
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
