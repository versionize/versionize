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

        public static implicit operator Options(VersionizeOptions options)
        {
            return new Options
            {
                SkipCommit = options.SkipCommit,
                SkipTag = options.SkipTag,
                SkipDirty = options.SkipDirty,
                DryRun = options.DryRun,
                WorkingDirectory = options.WorkingDirectory
            };
        }
    }
}

internal class RepoStateValidator : IRepoStateValidator
{
    internal static readonly string[] IgnoredToolDirectories =
    [
        ".claude",
        ".agent",
        ".agents",
        ".cursor",
        ".winsurf",
        ".windsurf",
        ".opencode",
        ".codex"
    ];

    internal static string IgnoredToolDirectoryList => string.Join(", ", IgnoredToolDirectories);

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
        if (IsCommitConfigurationRequired(options) && !IsConfiguredForCommits(repository))
        {
            throw new VersionizeException(ErrorMessages.GitConfigMissing(), 1);
        }

        if (options.SkipCommit)
        {
            return;
        }

        var status = repository.RetrieveStatus(new StatusOptions { IncludeUntracked = false });
        var repositoryRoot = Path.GetFullPath(repository.Info.WorkingDirectory);

        var ignoredSymlinks = status
            .Where(entry => IsIgnoredToolSymlink(entry.FilePath, entry.State, repositoryRoot))
            .ToList();

        var ignoredExistingToolEntries = status
            .Where(entry => IsIgnoredExistingToolEntry(entry.FilePath, entry.State, repositoryRoot))
            .ToList();

        WarnAboutIgnoredSymlinks(ignoredSymlinks);
        WarnAboutIgnoredExistingToolEntries(ignoredExistingToolEntries);

        var ignoredPaths = ignoredSymlinks
            .Concat(ignoredExistingToolEntries)
            .Select(x => x.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dirtyFiles = status
            .Where(x => !HasState(x.State, FileStatus.Ignored) && !ignoredPaths.Contains(x.FilePath))
            .Select(x => $"{x.State}: {x.FilePath}")
            .ToList();

        if (dirtyFiles.Count != 0 && !options.SkipDirty)
        {
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

    /// <summary>
    /// Indicates whether git user name and email are configured.
    /// </summary>
    private static bool IsConfiguredForCommits(Repository repository)
    {
        var name = repository.Config.Get<string>("user.name");
        var email = repository.Config.Get<string>("user.email");

        return name != null && email != null;
    }

    internal static bool IsIgnoredToolSymlink(string filePath, FileStatus state, string repositoryRoot)
    {
        if (HasState(state, FileStatus.Ignored) || !OperatingSystem.IsWindows())
        {
            return false;
        }

        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, filePath));
        return IsToolDirectoryPath(fullPath, repositoryRoot) && IsWindowsReparsePoint(fullPath);
    }

    internal static bool IsIgnoredExistingToolEntry(string filePath, FileStatus state, string repositoryRoot)
    {
        if (!HasState(state, FileStatus.DeletedFromWorkdir) || !OperatingSystem.IsWindows())
        {
            return false;
        }

        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, filePath));
        return IsToolDirectoryPath(fullPath, repositoryRoot) && PathExists(fullPath);
    }

    internal static bool IsToolDirectoryPath(string fullPath, string repositoryRoot)
    {
        return IgnoredToolDirectories.Any(directory =>
        {
            var toolRoot = Path.Combine(repositoryRoot, directory);
            var normalizedToolRoot = EnsureTrailingSeparator(Path.GetFullPath(toolRoot));
            return fullPath.StartsWith(normalizedToolRoot, StringComparison.OrdinalIgnoreCase);
        });
    }

    internal static bool IsWindowsReparsePoint(string fullPath)
    {
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return false;
        }

        return File.GetAttributes(fullPath).HasFlag(FileAttributes.ReparsePoint);
    }

    internal static bool PathExists(string fullPath)
    {
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    private static bool HasState(FileStatus state, FileStatus expected)
    {
        return (state & expected) == expected;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static void WarnAboutIgnoredSymlinks(IReadOnlyCollection<StatusEntry> ignoredSymlinks)
    {
        if (ignoredSymlinks.Count == 0)
        {
            return;
        }

        CommandLineUI.Warning(InfoMessages.IgnoredToolSymlinks(ignoredSymlinks.Count, IgnoredToolDirectoryList));

        if (CommandLineUI.Verbosity < Versionize.CommandLine.LogLevel.All)
        {
            return;
        }

        foreach (var ignoredSymlink in ignoredSymlinks.OrderBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            CommandLineUI.Information(InfoMessages.ProjectFile(ignoredSymlink.FilePath));
        }
    }

    private static void WarnAboutIgnoredExistingToolEntries(IReadOnlyCollection<StatusEntry> ignoredExistingToolEntries)
    {
        if (ignoredExistingToolEntries.Count == 0)
        {
            return;
        }

        CommandLineUI.Warning(InfoMessages.IgnoredToolDirectoryEntries(ignoredExistingToolEntries.Count, IgnoredToolDirectoryList));

        if (CommandLineUI.Verbosity < Versionize.CommandLine.LogLevel.All)
        {
            return;
        }

        foreach (var ignoredEntry in ignoredExistingToolEntries.OrderBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            CommandLineUI.Information(InfoMessages.ProjectFile(ignoredEntry.FilePath));
        }
    }
}
