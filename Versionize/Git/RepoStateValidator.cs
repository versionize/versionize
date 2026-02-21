using LibGit2Sharp;
using Versionize.CommandLine;
using Versionize.Config;

namespace Versionize.Git;

internal interface IRepoStateValidator
{
    void Validate(Repository repository, Options options);

    sealed record Options
    {
        public required bool SkipCommit { get; init; }
        public required bool SkipTag { get; init; }
        public required bool SkipDirty { get; init; }
        public required bool DryRun { get; init; }
        public required string WorkingDirectory { get; init; }
        public string? GitUserName { get; init; }
        public string? GitUserEmail { get; init; }

        public static implicit operator Options(VersionizeOptions options)
        {
            return new Options
            {
                SkipCommit = options.SkipCommit,
                SkipTag = options.SkipTag,
                SkipDirty = options.SkipDirty,
                DryRun = options.DryRun,
                WorkingDirectory = options.WorkingDirectory,
                GitUserName = options.GitUserName,
                GitUserEmail = options.GitUserEmail,
            };
        }
    }
}

internal class RepoStateValidator(IGitIdentityResolver gitIdentityResolver) : IRepoStateValidator
{
    private readonly IGitIdentityResolver _gitIdentityResolver = gitIdentityResolver;

    /// <summary>
    /// Ensures<br/>
    /// - Git user config is set for commit/tag write operations.<br/>
    /// - The working tree has no pending changes that could interfere with commit/tag write operations.
    /// </summary>
    /// <remarks>
    /// The checks are skipped if write operations are disabled via options (e.g. SkipCommit, SkipTag, DryRun).
    /// </remarks>
    public void Validate(Repository repository, IRepoStateValidator.Options options)
    {
        if (IsCommitConfigurationRequired(options) &&
            !_gitIdentityResolver.IsConfigured(repository, options.GitUserName, options.GitUserEmail))
        {
            throw new VersionizeException(ErrorMessages.GitConfigMissing(), 1);
        }

        if (options.SkipCommit)
        {
            return;
        }

        var status = repository.RetrieveStatus(new StatusOptions { IncludeUntracked = false });
        if (status.IsDirty && !options.SkipDirty)
        {
            var dirtyFiles = status.Where(x => x.State != FileStatus.Ignored).Select(x => $"{x.State}: {x.FilePath}");
            var dirtyFilesString = string.Join(Environment.NewLine, dirtyFiles);
            throw new VersionizeException(ErrorMessages.RepositoryDirty(options.WorkingDirectory, dirtyFilesString), 1);
        }
    }

    /// <summary>
    /// Indicates whether git user configuration is required for this run.
    /// For example, if commits or tags need to be created then this returns true.
    /// </summary>
    private static bool IsCommitConfigurationRequired(IRepoStateValidator.Options options)
    {
        return (!options.SkipCommit || !options.SkipTag) && !options.DryRun;
    }
}
